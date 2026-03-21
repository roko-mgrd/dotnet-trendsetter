namespace Trendsetter.Engine.Scorers;

using Trendsetter.Engine.Models;

public sealed class ExactScorer : IScorer
{
    public ScoringMode Mode => ScoringMode.Exact;

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

        return string.Equals(
            expected.Trim().ToLowerInvariant(),
            actual.Trim().ToLowerInvariant(),
            StringComparison.Ordinal) ? 1.0 : 0.0;
    }
}
