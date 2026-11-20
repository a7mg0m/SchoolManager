using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public record UpdateVaccinationCampaignCommand(
        Guid Id,
        Guid VaccineId,
        string Title,
        DateOnly StartDate,
        DateOnly EndDate
    ) : ICommand<bool>;
}
