namespace Trendsetter.Engine.Reports;

using System.Text.Json;
using Trendsetter.Engine.Models;

/// <summary>
/// CLI-friendly runner for trend tests.
/// Handles running tests, saving results, and delegating to <see cref="ReportCommand"/>.
///
/// Usage:
///   RunCommand.RunAsync(args, tests, "reports/")
///
/// Commands:
///   (no args)                     → run all tests
///   --test &lt;testId&gt;              → run a single test by ID
///
/// After running, remaining args are forwarded to <see cref="ReportCommand"/>.
/// </summary>
public static class RunCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>
    /// A registered test: its ID and a delegate that runs it given optional history.
    /// </summary>
    public readonly record struct TestEntry(
        string TestId,
        Func<RunResult[]?, Task<RunResult>> RunAsync);

    public static async Task RunAsync(
        string[] args,
        IReadOnlyList<TestEntry> tests,
        string baseDirectory = "reports")
    {
        var generator = new ReportGenerator(baseDirectory);

        // Parse --test filter
        string? testFilter = null;
        var reportArgs = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "--test" && i + 1 < args.Length)
            {
                testFilter = args[++i];
            }
            else
            {
                reportArgs.Add(args[i]);
            }
        }

        foreach (var test in tests)
        {
            if (testFilter is not null &&
                !test.TestId.Equals(testFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Running trend test: {test.TestId}");
            Console.ResetColor();

            var existingReport = await generator.GenerateAsync(test.TestId);
            var history = existingReport?.Runs.ToArray();

            var result = await test.RunAsync(history);

            await SaveRunResultAsync(result, baseDirectory);
            PrintRunResult(result);
        }

        // Forward remaining args to ReportCommand
        if (reportArgs.Count > 0)
            await ReportCommand.RunAsync([.. reportArgs], baseDirectory);
    }

    private static async Task SaveRunResultAsync(RunResult result, string baseDirectory)
    {
        var dir = Path.Combine(baseDirectory, result.TestId.Replace('.', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, $"run_{result.RunNumber}.json");

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, result, JsonOptions);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Result saved: {filePath}");
        Console.ResetColor();
    }

    private static void PrintRunResult(RunResult result)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"═══ {result.TestId} — Run #{result.RunNumber} ═══");
        Console.ResetColor();
        Console.WriteLine($"  Timestamp : {result.Timestamp:u}");
        Console.WriteLine($"  Overall   : {result.Score:P1}");
        Console.WriteLine();

        for (var i = 0; i < result.Items.Count; i++)
        {
            var item = result.Items[i];
            Console.WriteLine($"  Item [{i}] — {item.Score:P1}");

            foreach (var field in item.FieldScores)
            {
                var color = field.Score >= 0.9 ? ConsoleColor.Green
                    : field.Score >= 0.5 ? ConsoleColor.Yellow
                    : ConsoleColor.Red;
                Console.ForegroundColor = color;
                Console.Write($"    {field.Score:P0}");
                Console.ResetColor();
                Console.WriteLine($"  {field.FieldName} ({field.Mode})");

                if (field.Score < 1.0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"         expected: {field.Expected}");
                    Console.WriteLine($"         actual  : {field.Actual}");
                    Console.ResetColor();
                }
            }

            Console.WriteLine();
        }
    }
}
