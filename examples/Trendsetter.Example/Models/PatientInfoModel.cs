namespace Trendsetter.Example.Models;

public record PatientInfoModel(
    string FullName,
    DateOnly DateOfBirth,
    string Gender,
    string MemberId
);
