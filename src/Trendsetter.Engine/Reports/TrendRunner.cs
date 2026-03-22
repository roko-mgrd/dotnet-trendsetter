namespace Trendsetter.Engine.Reports;

using System.Collections.Concurrent;
using System.Text.Json;
using Trendsetter.Engine.Models;

/// <summary>
/// Core execution engine for trend tests.
/// Supports sequential and concurrent execution with cancellation.
/// </summary>
public sealed class TrendRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly string _baseDirectory;
    private readonly ReportGenerator _generator;

    public TrendRunner(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
        _generator = new ReportGenerator(baseDirectory);
    }

    /// <summary>
    /// A registered test: its ID and a delegate that runs it given optional history.
    /// </summary>
    public readonly record struct TestEntry(
        string TestId,
        Type TestType,
        Func<RunResult[]?, Task<RunResult>> RunAsync);

    /// <summary>
    /// Raised when a test starts executing.
    /// </summary>
    public event Action<string>? TestStarted;

    /// <summary>
    /// Raised when a test finishes executing with a result.
    /// </summary>
    public event Action<RunResult>? TestCompleted;

    /// <summary>
    /// Raised when a test throws an exception.
    /// </summary>
    public event Action<string, Exception>? TestFailed;

    /// <summary>
    /// Run trend tests with optional filtering and concurrency.
    /// Tests that fail are reported via <see cref="TestFailed"/> and excluded from the returned list.
    /// </summary>
    public async Task<IReadOnlyList<RunResult>> RunAsync(
        IReadOnlyList<TestEntry> tests,
        RunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new RunOptions();

        var filtered = FilterTests(tests, options);

        if (filtered.Count == 0)
            return [];

        var maxConcurrency = Math.Max(1, options.MaxConcurrency);

        if (maxConcurrency == 1)
            return await RunSequentialAsync(filtered, cancellationToken);

        return await RunConcurrentAsync(filtered, maxConcurrency, cancellationToken);
    }

    private static IReadOnlyList<TestEntry> FilterTests(
        IReadOnlyList<TestEntry> tests,
        RunOptions options)
    {
        if (options.TestFilter is null)
            return tests;

        return tests
            .Where(t => t.TestId.Equals(options.TestFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task<IReadOnlyList<RunResult>> RunSequentialAsync(
        IReadOnlyList<TestEntry> tests,
        CancellationToken cancellationToken)
    {
        var results = new List<RunResult>(tests.Count);

        foreach (var test in tests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TestStarted?.Invoke(test.TestId);

            try
            {
                var result = await RunSingleAsync(test, cancellationToken);
                results.Add(result);
                TestCompleted?.Invoke(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                TestFailed?.Invoke(test.TestId, ex);
            }
        }

        return results;
    }

    private async Task<IReadOnlyList<RunResult>> RunConcurrentAsync(
        IReadOnlyList<TestEntry> tests,
        int maxConcurrency,
        CancellationToken cancellationToken)
    {
        var results = new ConcurrentBag<RunResult>();
        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        var tasks = tests.Select(async test =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                TestStarted?.Invoke(test.TestId);

                try
                {
                    var result = await RunSingleAsync(test, cancellationToken);
                    results.Add(result);
                    TestCompleted?.Invoke(result);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    TestFailed?.Invoke(test.TestId, ex);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return results.ToList();
    }

    private async Task<RunResult> RunSingleAsync(
        TestEntry test,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingReport = await _generator.GenerateAsync(test.TestId);
        var history = existingReport?.Runs.ToArray();

        var result = await test.RunAsync(history);

        await SaveRunResultAsync(result);
        await GenerateHtmlReportAsync(result.TestId);

        return result;
    }

    private async Task SaveRunResultAsync(RunResult result)
    {
        var dir = Path.Combine(
            _baseDirectory,
            result.TestId.Replace('.', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, $"run_{result.RunNumber}.json");

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, result, JsonOptions);
    }

    private async Task GenerateHtmlReportAsync(string testId)
    {
        var report = await _generator.GenerateAsync(testId);
        if (report is null)
            return;

        var writer = new HtmlReportWriter();
        await writer.WriteAsync(report);
    }
}
