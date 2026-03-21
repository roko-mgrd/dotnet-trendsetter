namespace Trendsetter.Example.Models;

public record ProcedureModel(
    string Name,
    string ShortDescription,
    DateOnly DateOfTreatment,
    string ProviderName,
    List<DiagnosisModel> Diagnoses
);
