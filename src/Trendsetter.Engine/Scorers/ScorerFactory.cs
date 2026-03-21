namespace Trendsetter.Engine.Scorers;

using Trendsetter.Engine.Models;

public sealed class ScorerFactory
{
    private readonly ISentenceEncoder? _encoder;

    public ScorerFactory(ISentenceEncoder? encoder = null)
        => _encoder = encoder;

    public IScorer Get(ScoringMode mode)
    {
        return mode switch
        {
            ScoringMode.Exact => new ExactScorer(),
            ScoringMode.Partial => new PartialScorer(),
            ScoringMode.Semantic => new SemanticScorer(_encoder),
            ScoringMode.Structural => new StructuralScorer(),
            ScoringMode.Skip => new SkipScorer(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}
