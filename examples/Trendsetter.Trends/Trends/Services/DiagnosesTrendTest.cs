namespace Trendsetter.Trends.Trends.Services;

using Trendsetter.Engine.Builders;
using Trendsetter.Engine.Contracts;
using Trendsetter.Trends.Configuration;
using Trendsetter.Example.Models;
using Trendsetter.Example.Services;

public class DiagnosesTrendTest : TrendTest<DiagnosisModel, IReadOnlyList<DiagnosisModel>>
{
    private readonly IMyAiService _aiService;

    public DiagnosesTrendTest(IMyAiService aiService)
    {
        _aiService = aiService;
    }

    public override void Configure(TrendModelBuilder<DiagnosisModel> builder)
    {
        new DiagnosisConfiguration().Configure(builder);
    }

    public override Task<IReadOnlyList<DiagnosisModel>> GetExpectedAsync()
    {
        return Task.FromResult<IReadOnlyList<DiagnosisModel>>([
            new DiagnosisModel("M75.110", "Rotator cuff tear or rupture of right shoulder"),
            new DiagnosisModel("M75.100", "Rotator cuff syndrome of right shoulder"),
            new DiagnosisModel("Z96.611", "Presence of right artificial shoulder joint"),
            new DiagnosisModel("M25.511", "Pain in right shoulder"),
            new DiagnosisModel("E11.9", "Type 2 diabetes mellitus without complications"),
            new DiagnosisModel("I10", "Essential hypertension"),
        ]);
    }

    public override async Task<IReadOnlyList<DiagnosisModel>> GetActualAsync()
    {
        return await _aiService.ExtractDiagnosesAsync(SampleMedicalRecord.Text);
    }
}
