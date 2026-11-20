using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>Creates the student's medical profile, or updates it when the student has one</summary>
    public record SaveStudentMedicalProfileCommand(
        Guid StudentId,
        string? BloodType,
        string? Allergies,
        string? ChronicDiseases,
        string? RegularMedications,
        string? EmergencyPhone,
        string? Notes
    ) : ICommand<Guid>;
}
