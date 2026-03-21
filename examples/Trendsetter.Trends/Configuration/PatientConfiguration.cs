namespace Trendsetter.Trends.Configuration;

using Trendsetter.Engine.Builders;
using Trendsetter.Engine.Contracts;
using Trendsetter.Engine.Models;
using Trendsetter.Example.Models;

public class PatientConfiguration : ITrendConfiguration<PatientModel>
{
    public void Configure(TrendModelBuilder<PatientModel> builder)
    {
        builder
            .OwnsOne(x => x.PatientInfo, pi =>
            {
                pi.Property(x => x.FullName).HasScoringMode(ScoringMode.Partial);
                pi.Property(x => x.DateOfBirth).HasScoringMode(ScoringMode.Exact);
                pi.Property(x => x.Gender).HasScoringMode(ScoringMode.Exact);
                pi.Property(x => x.MemberId).HasScoringMode(ScoringMode.Exact);
            })
            .OwnsOne(x => x.Insurance, ins =>
            {
                ins.Property(x => x.PayerName).HasScoringMode(ScoringMode.Partial);
                ins.Property(x => x.PlanType).HasScoringMode(ScoringMode.Exact);
                ins.Property(x => x.GroupNumber).HasScoringMode(ScoringMode.Exact);
            })
            .OwnsMany(x => x.Procedures, proc =>
            {
                proc.Property(x => x.Name).HasScoringMode(ScoringMode.Exact);
                proc.Property(x => x.ShortDescription).HasScoringMode(ScoringMode.Semantic);
                proc.Property(x => x.DateOfTreatment).HasScoringMode(ScoringMode.Exact);
                proc.Property(x => x.ProviderName).HasScoringMode(ScoringMode.Partial);
                proc.OwnsMany(x => x.Diagnoses, dx =>
                {
                    dx.Property(x => x.Code).HasScoringMode(ScoringMode.Exact);
                    dx.Property(x => x.Description).HasScoringMode(ScoringMode.Semantic);
                });
            })
            .OwnsMany(x => x.Diagnoses, dx =>
            {
                dx.Property(x => x.Code).HasScoringMode(ScoringMode.Exact);
                dx.Property(x => x.Description).HasScoringMode(ScoringMode.Semantic);
            });
    }
}
