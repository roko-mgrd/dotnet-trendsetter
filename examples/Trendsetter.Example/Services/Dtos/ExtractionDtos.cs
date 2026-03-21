namespace Trendsetter.Example.Services.Dtos;

using System.ComponentModel;

[Description("List of extracted procedures")]
internal sealed class ProcedureListDto
{
    [Description("All procedures found in the medical record")]
    public List<ProcedureDto>? Procedures { get; init; }
}

[Description("List of extracted diagnoses")]
internal sealed class DiagnosisListDto
{
    [Description("All ICD-10 diagnoses found in the medical record")]
    public List<DiagnosisDto>? Diagnoses { get; init; }
}

internal sealed class PatientInfoDto
{
    [Description("Patient full name")]
    public string? FullName { get; init; }

    [Description("Date of birth in yyyy-MM-dd format")]
    public string? DateOfBirth { get; init; }

    [Description("Patient gender")]
    public string? Gender { get; init; }

    [Description("Insurance member ID")]
    public string? MemberId { get; init; }
}

internal sealed class PatientDto
{
    [Description("Patient demographic information")]
    public PatientInfoDto? PatientInfo { get; init; }

    [Description("Insurance information")]
    public InsuranceDto? Insurance { get; init; }

    [Description("All procedures performed")]
    public List<ProcedureDto>? Procedures { get; init; }

    [Description("Master list of all ICD-10 diagnoses")]
    public List<DiagnosisDto>? Diagnoses { get; init; }
}

internal sealed class InsuranceDto
{
    [Description("Insurance payer name")]
    public string? PayerName { get; init; }

    [Description("Insurance plan type (e.g. PPO, HMO)")]
    public string? PlanType { get; init; }

    [Description("Insurance group number")]
    public string? GroupNumber { get; init; }
}

internal sealed class ProcedureDto
{
    [Description("Procedure name")]
    public string? Name { get; init; }

    [Description("Brief description of the procedure")]
    public string? ShortDescription { get; init; }

    [Description("Date of treatment in yyyy-MM-dd format")]
    public string? DateOfTreatment { get; init; }

    [Description("Name of the provider")]
    public string? ProviderName { get; init; }

    [Description("ICD-10 diagnoses associated with this procedure")]
    public List<DiagnosisDto>? Diagnoses { get; init; }
}

internal sealed class DiagnosisDto
{
    [Description("ICD-10 diagnosis code")]
    public string? Code { get; init; }

    [Description("Diagnosis description")]
    public string? Description { get; init; }
}
