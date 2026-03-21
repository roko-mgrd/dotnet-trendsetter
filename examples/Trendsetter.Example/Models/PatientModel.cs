namespace Trendsetter.Example.Models;

public record PatientModel(
    PatientInfoModel PatientInfo,
    InsuranceModel Insurance,
    List<ProcedureModel> Procedures,
    List<DiagnosisModel> Diagnoses
);
