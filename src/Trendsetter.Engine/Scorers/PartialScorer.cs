namespace Trendsetter.Engine.Scorers;

using Trendsetter.Engine.Models;

public sealed class PartialScorer : IScorer
{
    public ScoringMode Mode => ScoringMode.Partial;

    public double Score(string? expected, string? actual)
    {
        if (expected is null && actual is null)
        {
            return 1.0;
        }

        if (expected is null || actual is null)
        {
            return 0.0;
        }

        var expectedTokens = Tokenize(expected);
        var actualTokens = Tokenize(actual);

        if (expectedTokens.Count == 0 && actualTokens.Count == 0)
        {
            return 1.0;
        }

        if (expectedTokens.Count == 0 || actualTokens.Count == 0)
        {
            return 0.0;
        }

        var intersection = expectedTokens.Intersect(actualTokens).Count();
        var precision = (double)intersection / actualTokens.Count;
        var recall = (double)intersection / expectedTokens.Count;

        if (precision + recall == 0)
        {
            return 0.0;
        }

        return 2 * precision * recall / (precision + recall); // F1
    }

    private static HashSet<string> Tokenize(string input)
    {
        return [.. input.ToLowerInvariant().Split([' ', ',', '.', '-', '_'], StringSplitOptions.RemoveEmptyEntries)];
    }
}
