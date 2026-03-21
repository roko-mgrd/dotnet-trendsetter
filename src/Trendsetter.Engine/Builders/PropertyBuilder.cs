namespace Trendsetter.Engine.Builders;

using Trendsetter.Engine.Models;

public sealed class PropertyBuilder<TModel, TProperty>
{
    private readonly TrendModelBuilder<TModel> _parent;
    private readonly string _fieldName;

    internal PropertyBuilder(TrendModelBuilder<TModel> parent, string fieldName)
    {
        _parent = parent;
        _fieldName = fieldName;
    }

    public TrendModelBuilder<TModel> HasScoringMode(ScoringMode mode)
    {
        _parent.Configuration.FieldModes[_fieldName] = mode;
        return _parent;
    }

    public TrendModelBuilder<TModel> Skip()
    {
        return HasScoringMode(ScoringMode.Skip);
    }
}
