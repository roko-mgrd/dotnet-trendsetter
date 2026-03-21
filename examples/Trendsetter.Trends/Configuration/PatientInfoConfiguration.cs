namespace Trendsetter.Trends.Configuration;

using Trendsetter.Engine.Builders;
using Trendsetter.Engine.Contracts;
using Trendsetter.Engine.Models;
using Trendsetter.Example.Models;

public class PatientInfoConfiguration : ITrendConfiguration<PatientInfoModel>
{
    public void Configure(TrendModelBuilder<PatientInfoModel> builder)
    {
        builder
            .Property(x => x.FullName).HasScoringMode(ScoringMode.Partial)
            .Property(x => x.DateOfBirth).HasScoringMode(ScoringMode.Exact)
            .Property(x => x.Gender).HasScoringMode(ScoringMode.Exact)
            .Property(x => x.MemberId).HasScoringMode(ScoringMode.Exact);
    }
}
