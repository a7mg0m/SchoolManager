using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public record UpdateClinicVisitCommand(
        Guid Id,
        string DoctorName,
        DateTime VisitDate,
        string Complaint,
        decimal? Temperature,
        decimal? Weight,
        decimal? Height,
        string? Diagnosis,
        string? Treatment,
        VisitOutcome Outcome,
        bool ParentNotified,
        string? Notes,
        List<VisitMedicationDto> Medications
    ) : ICommand<bool>;
}
