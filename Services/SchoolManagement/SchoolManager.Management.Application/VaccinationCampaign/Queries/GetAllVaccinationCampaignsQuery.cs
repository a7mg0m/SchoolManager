using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAllVaccinationCampaignsQuery() : ICommand<List<VaccinationCampaignDto>>;
}
