namespace Trendsetter.TestAdapter;

using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

[DefaultExecutorUri(TrendTestExecutor.ExecutorUri)]
[FileExtension(".dll")]
public sealed class TrendTestDiscoverer : ITestDiscoverer
{
    public void DiscoverTests(
        IEnumerable<string> sources,
        IDiscoveryContext discoveryContext,
        IMessageLogger logger,
        ITestCaseDiscoverySink discoverySink)
    {
        foreach (var source in sources)
        {
            try
            {
                var testTypes = FindTrendTestTypes(source);
                foreach (var (type, testId) in testTypes)
                {
                    var testCase = new TestCase(testId, new Uri(TrendTestExecutor.ExecutorUri), source)
                    {
                        DisplayName = testId,
                        CodeFilePath = null,
                    };

                    discoverySink.SendTestCase(testCase);
                }
            }
            catch (Exception ex)
            {
                logger.SendMessage(TestMessageLevel.Warning,
                    $"Trendsetter: Failed to scan {source}: {ex.Message}");
            }
        }
    }

    internal static IReadOnlyList<(Type Type, string TestId)> FindTrendTestTypes(string source)
    {
        var assembly = Assembly.LoadFrom(source);
        var results = new List<(Type, string)>();

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            if (!IsTrendTest(type))
                continue;

            var testId = type.FullName;
            if (testId is not null)
                results.Add((type, testId));
        }

        return results;
    }

    private static bool IsTrendTest(Type type)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType &&
                current.GetGenericTypeDefinition().FullName ==
                    "Trendsetter.Engine.Contracts.TrendTest`2")
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }
}
