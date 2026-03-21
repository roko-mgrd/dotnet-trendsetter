namespace Trendsetter.Engine.Models;

public sealed class RunResult
{
    public string TestId { get; init; } = string.Empty;
    public int RunNumber { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public double Score => Items.Count == 0
        ? 0
        : Items.Average(i => i.Score);

    public IReadOnlyList<ItemResult> Items { get; init; } = [];
}
