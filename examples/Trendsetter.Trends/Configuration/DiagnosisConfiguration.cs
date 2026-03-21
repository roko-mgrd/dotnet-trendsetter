namespace Trendsetter.Trends.Configuration;

using Trendsetter.Engine.Builders;
using Trendsetter.Engine.Contracts;
using Trendsetter.Engine.Models;
using Trendsetter.Example.Models;

public class DiagnosisConfiguration : ITrendConfiguration<DiagnosisModel>
{
    public void Configure(TrendModelBuilder<DiagnosisModel> builder)
    {
        builder
            .Property(x => x.Code).HasScoringMode(ScoringMode.Exact)
            .Property(x => x.Description).HasScoringMode(ScoringMode.Semantic);
    }
}
