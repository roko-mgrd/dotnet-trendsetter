namespace Trendsetter.Trends.Configuration;

using Trendsetter.Engine.Builders;
using Trendsetter.Engine.Contracts;
using Trendsetter.Engine.Models;
using Trendsetter.Example.Models;

public class ProcedureConfiguration : ITrendConfiguration<ProcedureModel>
{
    public void Configure(TrendModelBuilder<ProcedureModel> builder)
    {
        builder
            .Property(x => x.Name).HasScoringMode(ScoringMode.Exact)
            .Property(x => x.ShortDescription).HasScoringMode(ScoringMode.Semantic)
            .Property(x => x.DateOfTreatment).HasScoringMode(ScoringMode.Exact)
            .Property(x => x.ProviderName).HasScoringMode(ScoringMode.Partial)
            .OwnsMany(x => x.Diagnoses, db =>
            {
                db.Property(x => x.Code).HasScoringMode(ScoringMode.Exact);
                db.Property(x => x.Description).HasScoringMode(ScoringMode.Semantic);
            });
    }
}
