namespace Trendsetter.TestAdapter;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Trendsetter.Engine.Models;
using Trendsetter.Engine.Reports;

[ExtensionUri(ExecutorUri)]
public sealed class TrendTestExecutor : ITestExecutor
{
    public const string ExecutorUri = "executor://trendsetter/v1";

    private CancellationTokenSource? _cts;

    public void Cancel()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// Called by "dotnet test" when running tests from sources.
    /// Discovers tests and applies any --filter expression before executing.
    /// </summary>
    public void RunTests(
        IEnumerable<string>? sources,
        IRunContext? runContext,
        IFrameworkHandle? frameworkHandle)
    {
        if (sources is null || frameworkHandle is null)
            return;

        foreach (var source in sources)
        {
            var testTypes = TrendTestDiscoverer.FindTrendTestTypes(source);

            var testCases = testTypes.Select(t => new TestCase(
                t.TestId,
                new Uri(ExecutorUri),
                source)
            {
                DisplayName = t.TestId,
            }).ToList();

            var filtered = ApplyTestCaseFilter(testCases, runContext, frameworkHandle);
            RunTests(filtered, runContext, frameworkHandle);
        }
    }

    /// <summary>
    /// Called by "dotnet test" when running specific tests (filtered).
    /// </summary>
    public void RunTests(
        IEnumerable<TestCase>? tests,
        IRunContext? runContext,
        IFrameworkHandle? frameworkHandle)
    {
        if (tests is null || frameworkHandle is null)
            return;
        _cts = new CancellationTokenSource();

        // Group by source assembly so we build one DI container per assembly
        var bySource = tests.GroupBy(t => t.Source);

        foreach (var group in bySource)
        {
            if (_cts.IsCancellationRequested)
                break;

            ServiceProvider? sp = null;
            try
            {
                var assembly = Assembly.LoadFrom(group.Key);
                sp = BuildServiceProvider(assembly);

                foreach (var testCase in group)
                {
                    if (_cts.IsCancellationRequested)
                        break;
                    RunSingleTest(testCase, assembly, sp, frameworkHandle);
                }
            }
            catch (Exception ex)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error,
                    $"Trendsetter: Failed to initialize {group.Key}: {ex.Message}");

                foreach (var testCase in group)
                {
                    var result = new TestResult(testCase)
                    {
                        Outcome = TestOutcome.Failed,
                        ErrorMessage = $"DI initialization failed: {ex.Message}",
                    };
                    frameworkHandle.RecordResult(result);
                }
            }
            finally
            {
                sp?.Dispose();
            }
        }
    }

    private void RunSingleTest(
        TestCase testCase,
        Assembly assembly,
        IServiceProvider serviceProvider,
        IFrameworkHandle frameworkHandle)
    {
        frameworkHandle.RecordStart(testCase);
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            // Find the type by matching TestId
            var testTypes = TrendTestDiscoverer.FindTrendTestTypes(testCase.Source);
            var match = testTypes.FirstOrDefault(t => t.TestId == testCase.DisplayName);

            if (match.Type is null)
            {
                frameworkHandle.RecordResult(new TestResult(testCase)
                {
                    Outcome = TestOutcome.NotFound,
                    ErrorMessage = $"Test type not found for '{testCase.DisplayName}'",
                });
                return;
            }

            // Resolve via DI
            var testInstance = serviceProvider.GetRequiredService(match.Type);

            // Call RunAsync via reflection (it's on the base class)
            var runMethod = match.Type.GetMethod("RunAsync",
                BindingFlags.Public | BindingFlags.Instance,
                [typeof(RunResult[])]);

            if (runMethod is null)
            {
                frameworkHandle.RecordResult(new TestResult(testCase)
                {
                    Outcome = TestOutcome.Failed,
                    ErrorMessage = "Could not find RunAsync method on test type",
                });
                return;
            }

            // Delegate to TrendRunner so history is loaded and the result is persisted
            var baseDir = ResolveBaseDirectory(testCase.Source);
            var runner = new TrendRunner(baseDir);

            var entry = new TrendRunner.TestEntry(
                match.TestId,
                match.Type,
                history => (Task<RunResult>)runMethod.Invoke(testInstance, [history])!);

            var results = runner.RunAsync([entry]).GetAwaiter().GetResult();
            var runResult = results[0];

            var endTime = DateTimeOffset.UtcNow;

            var result = new TestResult(testCase)
            {
                Outcome = TestOutcome.Passed,
                Duration = endTime - startTime,
                StartTime = startTime,
                EndTime = endTime,
            };

            // Add score as a message
            result.Messages.Add(new TestResultMessage(
                TestResultMessage.StandardOutCategory,
                $"Score: {runResult.Score:P1} ({runResult.Items.Count} items)"));

            // Report per-item detail
            foreach (var (item, i) in runResult.Items.Select((item, i) => (item, i)))
            {
                var detail = string.Join("\n", item.FieldScores.Select(f =>
                    $"  {f.FieldName}: {f.Score:P0} ({f.Mode})"));

                result.Messages.Add(new TestResultMessage(
                    TestResultMessage.StandardOutCategory,
                    $"Item [{i}] — {item.Score:P1}\n{detail}"));
            }

            frameworkHandle.RecordResult(result);
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException ?? ex;
            frameworkHandle.RecordResult(new TestResult(testCase)
            {
                Outcome = TestOutcome.Failed,
                ErrorMessage = inner.Message,
                ErrorStackTrace = inner.StackTrace,
                Duration = DateTimeOffset.UtcNow - startTime,
            });
        }
    }

    /// <summary>
    /// Apply the --filter expression from <paramref name="runContext"/> to the discovered tests.
    /// Returns all tests when no filter is present.
    /// </summary>
    private static List<TestCase> ApplyTestCaseFilter(
        List<TestCase> testCases,
        IRunContext? runContext,
        IFrameworkHandle frameworkHandle)
    {
        if (runContext is null)
            return testCases;

        ITestCaseFilterExpression? filter;
        try
        {
            filter = runContext.GetTestCaseFilter(
                SupportedFilterProperties,
                name => SupportedFilterPropertyMap.GetValueOrDefault(name));
        }
        catch (TestPlatformFormatException ex)
        {
            frameworkHandle.SendMessage(TestMessageLevel.Warning,
                $"Trendsetter: Invalid filter expression: {ex.Message}");
            return testCases;
        }

        if (filter is null)
            return testCases;

        return testCases
            .Where(tc => filter.MatchTestCase(tc, name => name switch
            {
                "FullyQualifiedName" => tc.FullyQualifiedName,
                "DisplayName" => tc.DisplayName,
                _ => null,
            }))
            .ToList();
    }

    private static readonly string[] SupportedFilterProperties =
        ["FullyQualifiedName", "DisplayName"];

    private static readonly Dictionary<string, TestProperty> SupportedFilterPropertyMap = new()
    {
        ["FullyQualifiedName"] = TestCaseProperties.FullyQualifiedName,
        ["DisplayName"] = TestCaseProperties.DisplayName,
    };

    /// <summary>
    /// Resolve the reports base directory relative to the test project root.
    /// Navigates up from bin/Debug/net9.0 to the project directory, then into "reports".
    /// </summary>
    private static string ResolveBaseDirectory(string sourceAssemblyPath)
    {
        var binDir = Path.GetDirectoryName(sourceAssemblyPath)!;
        var projectDir = Path.GetFullPath(Path.Combine(binDir, "..", "..", ".."));
        return Path.Combine(projectDir, "reports");
    }

    private static ServiceProvider BuildServiceProvider(Assembly testAssembly)
    {
        var services = new ServiceCollection();

        // Find ITrendTestStartup implementation
        var startupType = testAssembly.GetTypes()
            .FirstOrDefault(t =>
                !t.IsAbstract &&
                !t.IsInterface &&
                t.GetInterfaces().Any(i => i.FullName == "Trendsetter.TestAdapter.ITrendTestStartup"));

        if (startupType is not null)
        {
            var startup = (ITrendTestStartup)Activator.CreateInstance(startupType)!;
            startup.ConfigureServices(services);
        }

        // Auto-register all TrendTest<,> types found in the assembly
        foreach (var type in testAssembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            var current = type.BaseType;
            while (current is not null)
            {
                if (current.IsGenericType &&
                    current.GetGenericTypeDefinition().FullName ==
                        "Trendsetter.Engine.Contracts.TrendTest`2")
                {
                    services.AddTransient(type);
                    break;
                }

                current = current.BaseType;
            }
        }

        return services.BuildServiceProvider();
    }
}
