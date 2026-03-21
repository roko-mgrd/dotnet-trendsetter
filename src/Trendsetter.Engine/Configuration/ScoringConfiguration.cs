namespace Trendsetter.Engine.Configuration;

using Trendsetter.Engine.Models;

/// <summary>
/// Holds the resolved scoring configuration for a single type T.
/// Built by TrendModelBuilder[T] and consumed by the scoring engine.
/// </summary>
public sealed class ScoringConfiguration
{
    // Field name -> ScoringMode for scalar properties
    public Dictionary<string, ScoringMode> FieldModes { get; } = [];

    // Field name -> nested ScoringConfiguration for owned single objects
    public Dictionary<string, ScoringConfiguration> OwnedOne { get; } = [];

    // Field name -> nested ScoringConfiguration for owned collections
    public Dictionary<string, ScoringConfiguration> OwnedMany { get; } = [];

    // Default mode used when a field has no explicit override
    public ScoringMode DefaultMode { get; set; } = ScoringMode.Partial;
}
