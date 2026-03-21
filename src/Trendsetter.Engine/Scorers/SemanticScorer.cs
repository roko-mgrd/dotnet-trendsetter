namespace Trendsetter.Engine.Scorers;

using Trendsetter.Engine.Models;

/// <summary>
/// Semantic scorer stub — wire up a real embeddings provider via ISentenceEncoder.
/// </summary>
public sealed class SemanticScorer : IScorer
{
    private readonly ISentenceEncoder? _encoder;

    public SemanticScorer(ISentenceEncoder? encoder = null)
        => _encoder = encoder;

    public ScoringMode Mode => ScoringMode.Semantic;

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

        if (_encoder is null)
        {
            return new PartialScorer().Score(expected, actual); // graceful fallback
        }

        var a = _encoder.Encode(expected);
        var b = _encoder.Encode(actual);
        return CosineSimilarity(a, b);
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return normA == 0 || normB == 0 ? 0 : dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
