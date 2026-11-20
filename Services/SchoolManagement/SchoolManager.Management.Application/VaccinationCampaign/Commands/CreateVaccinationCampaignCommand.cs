using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public record CreateVaccinationCampaignCommand(
        Guid VaccineId,
        string Title,
        DateOnly StartDate,
        DateOnly EndDate
    ) : ICommand<Guid>;
}
