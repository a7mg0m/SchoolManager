using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public record UpdateStudentVaccinationCommand(
        Guid Id,
        Guid VaccineId,
        int DoseNumber,
        VaccinationStatus Status,
        DateOnly? VaccinationDate,
        VaccinationPlace Place,
        Guid? CampaignId,
        Guid? GivenByEmployeeId,
        string? Notes
    ) : ICommand<bool>;
}
