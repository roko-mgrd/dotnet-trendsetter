namespace Trendsetter.Engine.Reports;

/// <summary>
/// Controls how trend tests are executed by <see cref="TrendRunner"/>.
/// </summary>
public sealed class RunOptions
{
    /// <summary>
    /// When set, only run the test whose ID matches (case-insensitive).
    /// When null, all tests are run.
    /// </summary>
    public string? TestFilter { get; init; }

    /// <summary>
    /// Maximum number of tests to run in parallel.
    /// 1 = sequential (default), >1 = concurrent.
    /// </summary>
    public int MaxConcurrency { get; init; } = 1;
}
