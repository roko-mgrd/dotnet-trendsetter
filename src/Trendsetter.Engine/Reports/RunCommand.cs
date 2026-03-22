namespace Trendsetter.Engine.Reports;

using Trendsetter.Engine.Models;

/// <summary>
/// CLI-friendly runner for trend tests.
/// Thin wrapper around <see cref="TrendRunner"/> that adds console output.
///
/// Usage:
///   RunCommand.RunAsync(args, tests, "reports/")
///
/// Commands:
///   (no args)                     → run all tests
///   --test &lt;testId&gt;              → run a single test by ID
///   --concurrency &lt;n&gt;            → run up to n tests in parallel
///
/// After running, remaining args are forwarded to <see cref="ReportCommand"/>.
/// </summary>
public static class RunCommand
{
    public static async Task RunAsync(
        string[] args,
        IReadOnlyList<TrendRunner.TestEntry> tests,
        string baseDirectory = "reports")
    {
        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true; // prevent immediate kill, allow graceful shutdown
            cts.Cancel();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nCancellation requested — finishing current test…");
            Console.ResetColor();
        };

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException) { }
        };

        // Parse CLI args
        string? testFilter = null;
        int maxConcurrency = 1;
        var reportArgs = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "--test" && i + 1 < args.Length)
            {
                testFilter = args[++i];
            }
            else if (args[i] is "--concurrency" && i + 1 < args.Length &&
                     int.TryParse(args[i + 1], out var c) && c > 0)
            {
                maxConcurrency = c;
                i++;
            }
            else
            {
                reportArgs.Add(args[i]);
            }
        }

        var runner = new TrendRunner(baseDirectory);

        runner.TestStarted += testId =>
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Running trend test: {testId}");
            Console.ResetColor();
        };

        runner.TestCompleted += result =>
        {
            PrintRunResult(result);
        };

        runner.TestFailed += (testId, ex) =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAILED: {testId} — {ex.Message}");
            Console.ResetColor();
        };

        var options = new RunOptions
        {
            TestFilter = testFilter,
            MaxConcurrency = maxConcurrency,
        };

        try
        {
            await runner.RunAsync(tests, options, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Run cancelled.");
            Console.ResetColor();
            return;
        }

        // Forward remaining args to ReportCommand
        if (reportArgs.Count > 0)
            await ReportCommand.RunAsync([.. reportArgs], baseDirectory);
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
