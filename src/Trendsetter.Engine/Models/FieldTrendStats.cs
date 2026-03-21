namespace Trendsetter.Engine.Models;

public sealed class FieldTrendStats
{
    public string FieldName { get; init; } = string.Empty;
    public double Mean { get; init; }
    public double StdDev { get; init; }
    public double Min { get; init; }
    public double Max { get; init; }
    public IReadOnlyList<double> History { get; init; } = [];
    public bool IsUnstable => StdDev > 0.15;
}