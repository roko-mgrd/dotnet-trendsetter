namespace Trendsetter.Engine.Models;

public record FieldScore
{
    public string FieldName { get; init; } = string.Empty;
    public double Score { get; init; }
    public ScoringMode Mode { get; init; }
    public string? Expected { get; init; }
    public string? Actual { get; init; }
}