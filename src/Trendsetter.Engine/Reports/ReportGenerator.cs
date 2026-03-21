namespace Trendsetter.Engine.Reports;

using System.Text.Json;
using Trendsetter.Engine.Models;

/// <summary>
/// Scans the reports directory tree and hydrates TrendReport objects.
/// Folder structure mirrors test IDs: "aws.procedures" → "aws/procedures/"
/// </summary>
public sealed class ReportGenerator
{
    private readonly string _baseDirectory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public ReportGenerator(string baseDirectory = "reports")
        => _baseDirectory = baseDirectory;

    /// <summary>
    /// Discovers all trend folders and returns one TrendReport per trend.
    /// </summary>
    public async Task<IReadOnlyList<TrendReport>> GenerateAllAsync()
    {
        if (!Directory.Exists(_baseDirectory))
            return [];

        var trendDirs = DiscoverTrendDirectories(_baseDirectory);
        var reports = new List<TrendReport>();

        foreach (var dir in trendDirs)
        {
            var report = await LoadTrendReportAsync(dir);
            if (report is not null)
                reports.Add(report);
        }

        return reports.OrderBy(r => r.TestId).ToList();
    }

    /// <summary>
    /// Generates a report for a single trend by test ID.
    /// </summary>
    public async Task<TrendReport?> GenerateAsync(string testId)
    {
        var dir = GetDirectory(testId);
        return await LoadTrendReportAsync(dir);
    }

    // Recursively finds all leaf directories that contain run_*.json files
    private IEnumerable<string> DiscoverTrendDirectories(string root)
    {
        foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
        {
            if (Directory.GetFiles(dir, "run_*.json").Length > 0)
                yield return dir;
        }
    }

    private async Task<TrendReport?> LoadTrendReportAsync(string directory)
    {
        if (!Directory.Exists(directory))
            return null;

        var files = Directory.GetFiles(directory, "run_*.json")
            .OrderBy(f =>
            {
                // Sort by run number extracted from filename: run_0.json → 0
                var name = Path.GetFileNameWithoutExtension(f);
                return int.TryParse(name.Replace("run_", ""), out var n) ? n : 0;
            })
            .ToList();

        if (files.Count == 0)
            return null;

        var runs = new List<RunResult>();

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var result = JsonSerializer.Deserialize<RunResult>(json, JsonOptions);
                if (result is not null)
                    runs.Add(result);
            }
            catch (JsonException ex)
            {
                // Skip malformed run files, log and continue
                Console.Error.WriteLine($"Warning: could not parse {file}: {ex.Message}");
            }
        }

        if (runs.Count == 0)
        {
            return null;
        }

        // Derive testId from the directory path relative to base
        var testId = runs[0].TestId
            ?? DirectoryToTestId(directory);

        var fieldStats = TrendAnalyzer.Analyze(runs);

        return new TrendReport
        {
            TestId = testId,
            DirectoryPath = directory,
            RelativePath = Path.GetRelativePath(_baseDirectory, directory),
            Runs = runs,
            FieldStats = fieldStats,
            LatestScore = runs.Last().Score,
            BestScore = runs.Max(r => r.Score),
            WorstScore = runs.Min(r => r.Score),
            ScoreHistory = runs.Select(r => r.Score).ToList(),
            UnstableFields = fieldStats.Where(f => f.IsUnstable).ToList()
        };
    }

    private string GetDirectory(string testId)
    {
        return Path.Combine(_baseDirectory, testId.Replace('.', Path.DirectorySeparatorChar));
    }

    private string DirectoryToTestId(string directory)
    {
        var rel = Path.GetRelativePath(_baseDirectory, directory);
        return rel.Replace(Path.DirectorySeparatorChar, '.').Replace('/', '.');
    }
}
