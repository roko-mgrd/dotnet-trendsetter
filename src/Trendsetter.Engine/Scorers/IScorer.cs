namespace Trendsetter.Engine.Scorers;

using Trendsetter.Engine.Models;

public interface IScorer
{
    ScoringMode Mode { get; }
    double Score(string? expected, string? actual);
}
