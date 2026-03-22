namespace Trendsetter.Trends.Trends.Services;

using Trendsetter.Engine.Builders;
using Trendsetter.Engine.Contracts;
using Trendsetter.Trends.Configuration;
using Trendsetter.Example.Models;
using Trendsetter.Example.Services;

public class PatientInfoTrendTest : TrendTest<PatientInfoModel, PatientInfoModel>
{
    private readonly IMyAiService _aiService;

    public PatientInfoTrendTest(IMyAiService aiService)
    {
        _aiService = aiService;
    }

    public override void Configure(TrendModelBuilder<PatientInfoModel> builder)
    {
        new PatientInfoConfiguration().Configure(builder);
    }

    public override Task<PatientInfoModel> GetExpectedAsync()
    {
        return Task.FromResult(
            new PatientInfoModel(
                FullName: "John Michael Doe",
                DateOfBirth: new DateOnly(1985, 6, 12),
                Gender: "Male",
                MemberId: "MBR-2024-98765"
            )
        );
    }

    public override async Task<PatientInfoModel> GetActualAsync()
    {
        return await _aiService.ExtractPatientInfoAsync(SampleMedicalRecord.Text);
    }
}
