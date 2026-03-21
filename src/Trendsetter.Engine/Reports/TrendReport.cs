namespace Trendsetter.Engine.Reports;

using Trendsetter.Engine.Models;

/// <summary>
/// All data needed to render one trend's HTML report.
/// Produced by ReportGenerator, consumed by HtmlReportWriter.
/// </summary>
public sealed class TrendReport
{
    public string TestId { get; init; } = string.Empty;

    /// <summary>Absolute path to the trend's folder on disk.</summary>
    public string DirectoryPath { get; init; } = string.Empty;

    /// <summary>Path relative to the reports base directory — used for breadcrumb navigation.</summary>
    public string RelativePath { get; init; } = string.Empty;

    public IReadOnlyList<RunResult> Runs { get; init; } = [];
    public IReadOnlyList<FieldTrendStats> FieldStats { get; init; } = [];
    public IReadOnlyList<FieldTrendStats> UnstableFields { get; init; } = [];

    public IReadOnlyList<double> ScoreHistory { get; init; } = [];

    public double LatestScore { get; init; }
    public double BestScore { get; init; }
    public double WorstScore { get; init; }

    public int RunCount => Runs.Count;

    /// <summary>
    /// Score delta between the last two runs.
    /// Positive = improved, negative = regressed.
    /// </summary>
    public double? ScoreDelta => Runs.Count >= 2
        ? Runs.Last().Score - Runs[^2].Score
        : null;

    public bool HasRegression => ScoreDelta < -0.05;
    public bool HasImprovement => ScoreDelta > 0.05;
}
