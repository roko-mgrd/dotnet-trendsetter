namespace Trendsetter.Trends.Trends.Services;

using Trendsetter.Engine.Builders;
using Trendsetter.Engine.Contracts;
using Trendsetter.Trends.Configuration;
using Trendsetter.Example.Models;
using Trendsetter.Example.Services;

public class ProceduresTrendTest : TrendTest<ProcedureModel, IReadOnlyList<ProcedureModel>>
{
    private readonly IMyAiService _aiService;

    public ProceduresTrendTest(IMyAiService aiService)
    {
        _aiService = aiService;
    }

    public override string TestId => "services.bedrock.procedures";

    public override void Configure(TrendModelBuilder<ProcedureModel> builder)
    {
        new ProcedureConfiguration().Configure(builder);
    }

    public override Task<IReadOnlyList<ProcedureModel>> GetExpectedAsync()
    {
        return Task.FromResult<IReadOnlyList<ProcedureModel>>([
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
        ]);
    }

    public override async Task<IReadOnlyList<ProcedureModel>> GetActualAsync()
    {
        return await _aiService.ExtractProceduresAsync(SampleMedicalRecord.Text);
    }
}
