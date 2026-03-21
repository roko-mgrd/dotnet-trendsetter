namespace Trendsetter.Engine.Scorers;

using Trendsetter.Engine.Models;

public sealed class StructuralScorer : IScorer
{
    public ScoringMode Mode => ScoringMode.Structural;

    public double Score(string? expected, string? actual)
    {
        // For lists serialized as JSON arrays, compute Jaccard on token sets.
        // For non-list values, fall back to PartialScorer.
        if (expected is null && actual is null)
        {
            return 1.0;
        }

        if (expected is null || actual is null)
        {
            return 0.0;
        }

        var expectedSet = Tokenize(expected);
        var actualSet = Tokenize(actual);

        if (expectedSet.Count == 0 && actualSet.Count == 0)
        {
            return 1.0;
        }

        var intersection = expectedSet.Intersect(actualSet).Count();
        var union = expectedSet.Union(actualSet).Count();
        return union == 0 ? 0.0 : (double)intersection / union;
    }

    private static HashSet<string> Tokenize(string input)
    {
        return [.. input.ToLowerInvariant().Split([' ', ',', '.', '-', '_', '[', ']', '"'], StringSplitOptions.RemoveEmptyEntries)];
    }
}
