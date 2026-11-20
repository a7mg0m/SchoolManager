using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetActiveVaccinationCampaignsQuery() : ICommand<List<VaccinationCampaignDto>>;
}
