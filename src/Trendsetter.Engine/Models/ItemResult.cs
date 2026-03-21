namespace Trendsetter.Engine.Models;

public sealed class ItemResult
{
    public double Score => FieldScores.Count == 0
        ? 0
        : FieldScores.Average(f => f.Score);

    public IReadOnlyList<FieldScore> FieldScores { get; init; } = [];
}
