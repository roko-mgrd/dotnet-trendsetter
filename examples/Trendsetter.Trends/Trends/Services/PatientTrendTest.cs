namespace Trendsetter.Trends.Trends.Services;

using Trendsetter.Engine.Builders;
using Trendsetter.Engine.Contracts;
using Trendsetter.Trends.Configuration;
using Trendsetter.Example.Models;
using Trendsetter.Example.Services;

public class PatientTrendTest : TrendTest<PatientModel, PatientModel>
{
    private readonly IMyAiService _aiService;

    public PatientTrendTest(IMyAiService aiService)
    {
        _aiService = aiService;
    }

    public override void Configure(TrendModelBuilder<PatientModel> builder)
    {
        new PatientConfiguration().Configure(builder);
    }

    public override Task<PatientModel> GetExpectedAsync()
    {
        return Task.FromResult(
            new PatientModel(
                PatientInfo: new PatientInfoModel(
                    FullName: "John Michael Doe",
                    DateOfBirth: new DateOnly(1985, 6, 12),
                    Gender: "Male",
                    MemberId: "MBR-2024-98765"
                ),
                Insurance: new InsuranceModel(
                    PayerName: "Blue Cross Blue Shield",
                    PlanType: "PPO",
                    GroupNumber: "GRP-445566"
                ),
                Procedures:
                [
                    new ProcedureModel(
                        Name: "Rotator cuff repair",
                        ShortDescription: "Surgical repair of torn rotator cuff tendon using arthroscopic technique",
                        DateOfTreatment: new DateOnly(2024, 3, 15),
                        ProviderName: "Dr. Sarah Smith",
                        Diagnoses:
                        [
                            new DiagnosisModel("M75.110", "Rotator cuff tear or rupture of right shoulder"),
                            new DiagnosisModel("M75.100", "Rotator cuff syndrome of right shoulder"),
                        ]),
                    new ProcedureModel(
                        Name: "Physical therapy evaluation",
                        ShortDescription: "Comprehensive PT evaluation for post-operative rotator cuff rehabilitation",
                        DateOfTreatment: new DateOnly(2024, 5, 22),
                        ProviderName: "Dr. James Wilson",
                        Diagnoses:
                        [
                            new DiagnosisModel("Z96.611", "Presence of right artificial shoulder joint"),
                            new DiagnosisModel("M25.511", "Pain in right shoulder"),
                        ]),
                ],
                Diagnoses:
                [
                    new DiagnosisModel("M75.110", "Rotator cuff tear or rupture of right shoulder"),
                    new DiagnosisModel("M75.100", "Rotator cuff syndrome of right shoulder"),
                    new DiagnosisModel("Z96.611", "Presence of right artificial shoulder joint"),
                    new DiagnosisModel("M25.511", "Pain in right shoulder"),
                    new DiagnosisModel("E11.9", "Type 2 diabetes mellitus without complications"),
                    new DiagnosisModel("I10", "Essential hypertension"),
                ]
            )
        );
    }

    public override async Task<PatientModel> GetActualAsync()
    {
        return await _aiService.ExtractPatientAsync(SampleMedicalRecord.Text);
    }
}
