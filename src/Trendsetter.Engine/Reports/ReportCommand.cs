namespace Trendsetter.Engine.Reports;

using Trendsetter.Engine.Models;

/// <summary>
/// CLI-friendly entry point.
/// Wire into a console app or a `dotnet trendsetter` tool.
///
/// Usage:
///   ReportCommand.RunAsync(args)
///
/// Commands:
///   (no args)                     → list all trends with saved results
///   dashboard                     → generate dashboard.html
///   dashboard --open              → generate and open in browser
///   &lt;testId&gt;                       → show latest run in console
///   &lt;testId&gt; --all                → show all runs
///   &lt;testId&gt; --run &lt;n&gt;            → show specific run
///   &lt;testId&gt; --trend              → show field-level trend stats
///   &lt;testId&gt; --html               → generate report.html for that trend
/// </summary>
public static class ReportCommand
{
    public static async Task RunAsync(string[] args, string baseDirectory = "reports")
    {
        var generator = new ReportGenerator(baseDirectory);

        if (args.Length == 0)
        {
            await ListTrendsAsync(generator);
            return;
        }

        var command = args[0].ToLowerInvariant();

        if (command == "dashboard")
        {
            var openFlag = args.Contains("--open");
            await GenerateDashboardAsync(generator, baseDirectory, openFlag);
            return;
        }

        // Otherwise first arg is a testId
        var testId = args[0];
        var flags = args.Skip(1).ToHashSet();

        var report = await generator.GenerateAsync(testId);
        if (report is null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"No results found for trend: {testId}");
            Console.ResetColor();
            return;
        }

        if (flags.Contains("--html"))
        {
            var writer = new HtmlReportWriter();
            var path = await writer.WriteAsync(report);
            Console.WriteLine($"Report written: {path}");
            return;
        }

        if (flags.Contains("--trend"))
        {
            PrintTrendStats(report);
            return;
        }

        if (flags.Contains("--all"))
        {
            foreach (var run in report.Runs)
                PrintRun(run);
            return;
        }

        if (flags.TryGetValue("--run", out _))
        {
            var idx = Array.IndexOf(args, "--run");
            if (idx >= 0 && idx + 1 < args.Length && int.TryParse(args[idx + 1], out var runNum))
            {
                var run = report.Runs.FirstOrDefault(r => r.RunNumber == runNum);
                if (run is null)
                {
                    Console.WriteLine($"Run {runNum} not found.");
                    return;
                }

                PrintRun(run);
                return;
            }
        }

        // Default: show latest run
        PrintRun(report.Runs.Last());
    }

    // -------------------------------------------------------------------------

    private static async Task ListTrendsAsync(ReportGenerator generator)
    {
        var reports = await generator.GenerateAllAsync();

        if (reports.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("No trend results found. Run your trend tests first.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine();
        WriteHeader("Trends");
        Console.WriteLine();

        foreach (var r in reports)
        {
            var scoreStr = $"{r.LatestScore:P1}".PadLeft(7);
            var runsStr = $"{r.RunCount} run{(r.RunCount != 1 ? "s" : "")}".PadLeft(8);
            var deltaStr = r.ScoreDelta.HasValue
                ? (r.ScoreDelta > 0 ? $"▲{r.ScoreDelta:P1}" : $"▼{Math.Abs(r.ScoreDelta.Value):P1}").PadLeft(8)
                : "        ";
            var unstable = r.UnstableFields.Count > 0 ? $" ⚡{r.UnstableFields.Count}" : "   ";

            Console.ForegroundColor = r.LatestScore >= 0.8 ? ConsoleColor.Green
                : r.LatestScore >= 0.5 ? ConsoleColor.Yellow : ConsoleColor.Red;
            Console.Write(scoreStr + " ");
            Console.ResetColor();

            Console.ForegroundColor = r.ScoreDelta > 0 ? ConsoleColor.Green
                : r.ScoreDelta < 0 ? ConsoleColor.Red : ConsoleColor.DarkGray;
            Console.Write(deltaStr + " ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(runsStr);
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(unstable + " ");
            Console.ResetColor();

            Console.WriteLine(" " + r.TestId);
        }

        Console.WriteLine();
    }

    private static async Task GenerateDashboardAsync(
        ReportGenerator generator, string baseDirectory, bool open)
    {
        Console.Write("Generating reports... ");
        var reports = await generator.GenerateAllAsync();

        // Generate per-trend reports
        var reportWriter = new HtmlReportWriter();
        foreach (var report in reports)
            await reportWriter.WriteAsync(report);

        // Generate dashboard
        var dashWriter = new HtmlDashboardWriter(baseDirectory);
        var path = await dashWriter.WriteAsync(reports);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("done");
        Console.ResetColor();
        Console.WriteLine($"Dashboard: {path}");
        Console.WriteLine($"Trend reports: {reports.Count} written");

        if (open)
            OpenInBrowser(path);
    }

    private static void PrintRun(RunResult run)
    {
        Console.WriteLine();
        WriteHeader($"Run {run.RunNumber}  —  {run.TestId}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  {run.Timestamp:yyyy-MM-dd HH:mm:ss UTC}");
        Console.ResetColor();
        Console.WriteLine();

        foreach (var item in run.Items)
        {
            Console.ForegroundColor = ScoreColor(item.Score);
            Console.WriteLine($"  Item score: {item.Score:P1}");
            Console.ResetColor();

            foreach (var field in item.FieldScores.OrderBy(f => f.FieldName))
            {
                if (field.Mode == ScoringMode.Skip)
                    continue;

                var bar = ScoreBar(field.Score, 20);
                var mode = field.Mode.ToString().ToLowerInvariant().PadRight(10);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"    {field.FieldName,-32} ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(mode + " ");
                Console.ForegroundColor = ScoreColor(field.Score);
                Console.Write(bar + " ");
                Console.Write($"{field.Score:P1}");
                Console.ResetColor();
                Console.WriteLine();
            }

            Console.WriteLine();
        }

        Console.ForegroundColor = ScoreColor(run.Score);
        Console.WriteLine($"  Overall: {run.Score:P1}");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void PrintTrendStats(TrendReport report)
    {
        Console.WriteLine();
        WriteHeader($"Field Trends  —  {report.TestId}");
        Console.WriteLine();

        foreach (var field in report.FieldStats.OrderBy(f => f.FieldName))
        {
            var unstable = field.IsUnstable ? " ⚡" : "  ";
            var sparkline = Sparkline(field.History);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  {field.FieldName,-36} ");
            Console.ForegroundColor = ScoreColor(field.Mean);
            Console.Write($"{field.Mean:P1} ");
            Console.ForegroundColor = field.IsUnstable ? ConsoleColor.Yellow : ConsoleColor.DarkGray;
            Console.Write($"±{field.StdDev:F3} ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(sparkline);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(unstable);
            Console.ResetColor();
            Console.WriteLine();
        }

        Console.WriteLine();
    }

    // -------------------------------------------------------------------------

    private static void WriteHeader(string text)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("  ═══ ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(text);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" ═══");
        Console.ResetColor();
    }

    private static ConsoleColor ScoreColor(double score)
    {
        return score < 0.8 ? score >= 0.5 ? ConsoleColor.Yellow : ConsoleColor.Red : ConsoleColor.Green;
    }

    private static string ScoreBar(double score, int width)
    {
        var filled = (int)Math.Round(score * width);
        return "[" + new string('█', filled) + new string('░', width - filled) + "]";
    }

    private static string Sparkline(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return string.Empty;
        var chars = new[] { '▁', '▂', '▃', '▄', '▅', '▆', '▇', '█' };
        return new string(values.Select(v => chars[(int)Math.Min(v * 8, 7)]).ToArray());
    }

    private static void OpenInBrowser(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var uri = new Uri(fullPath).AbsoluteUri;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Could not auto-open browser. Open manually: {path}");
            Console.ResetColor();
        }
    }
}
