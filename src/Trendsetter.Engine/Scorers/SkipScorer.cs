namespace Trendsetter.Engine.Scorers;

using Trendsetter.Engine.Models;

public sealed class SkipScorer : IScorer
{
    public ScoringMode Mode => ScoringMode.Skip;
    public double Score(string? expected, string? actual)
    {
        return 1.0; // skipped fields don't penalize
    }
}
