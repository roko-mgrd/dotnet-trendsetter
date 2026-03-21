namespace Trendsetter.Engine.Builders;
using System.Linq.Expressions;
using Trendsetter.Engine.Configuration;
using Trendsetter.Engine.Models;

public sealed class TrendModelBuilder<TModel>
{
    internal ScoringConfiguration Configuration { get; } = new();

    /// <summary>
    /// Configure scoring for a scalar property.
    /// builder.Property(x => x.Name).HasScoringMode(ScoringMode.Exact)
    /// </summary>
    public PropertyBuilder<TModel, TProperty> Property<TProperty>(
        Expression<Func<TModel, TProperty>> propertyExpression)
    {
        var name = GetMemberName(propertyExpression);
        return new PropertyBuilder<TModel, TProperty>(this, name);
    }

    /// <summary>
    /// Configure scoring for a nested single owned object.
    /// builder.OwnsOne(x => x.Address, ab => { ab.Property(x => x.City).HasScoringMode(ScoringMode.Exact); })
    /// </summary>
    public TrendModelBuilder<TModel> OwnsOne<TOwned>(
        Expression<Func<TModel, TOwned>> navigationExpression,
        Action<TrendModelBuilder<TOwned>> configure)
    {
        var name = GetMemberName(navigationExpression);
        var nested = new TrendModelBuilder<TOwned>();
        configure(nested);
        Configuration.OwnedOne[name] = nested.Configuration;
        return this;
    }

    /// <summary>
    /// Configure scoring for a nested collection of owned objects.
    /// builder.OwnsMany(x => x.Diagnoses, db => { db.Property(x => x.Code).HasScoringMode(ScoringMode.Exact); })
    /// </summary>
    public TrendModelBuilder<TModel> OwnsMany<TOwned>(
        Expression<Func<TModel, IEnumerable<TOwned>>> navigationExpression,
        Action<TrendModelBuilder<TOwned>> configure)
    {
        var name = GetMemberName(navigationExpression);
        var nested = new TrendModelBuilder<TOwned>();
        configure(nested);
        Configuration.OwnedMany[name] = nested.Configuration;
        return this;
    }

    /// <summary>
    /// Set the default scoring mode applied to any field not explicitly configured.
    /// </summary>
    public TrendModelBuilder<TModel> HasDefaultScoringMode(ScoringMode mode)
    {
        Configuration.DefaultMode = mode;
        return this;
    }

    private static string GetMemberName<T, TProp>(Expression<Func<T, TProp>> expression)
    {
        if (expression.Body is MemberExpression member)
        {
            return member.Member.Name;
        }

        throw new ArgumentException(
            $"Expression '{expression}' does not refer to a property or field.");
    }
}