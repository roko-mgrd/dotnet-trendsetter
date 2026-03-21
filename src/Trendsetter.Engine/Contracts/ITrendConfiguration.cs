namespace Trendsetter.Engine.Contracts;

using global::Trendsetter.Engine.Builders;

/// <summary>
/// Implement this to define scoring configuration for a model type.
/// Register it with TrendModelRegistry.
/// </summary>
public interface ITrendConfiguration<TModel>
{
    void Configure(TrendModelBuilder<TModel> builder);
}
