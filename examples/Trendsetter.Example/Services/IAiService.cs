namespace Trendsetter.Example.Services;

using Trendsetter.Example.Models;

public interface IMyAiService
{
    Task<IReadOnlyList<ProcedureModel>> ExtractProceduresAsync(string input);
    Task<IReadOnlyList<DiagnosisModel>> ExtractDiagnosesAsync(string input);
    Task<PatientInfoModel> ExtractPatientInfoAsync(string input);
    Task<PatientModel> ExtractPatientAsync(string input);
}
