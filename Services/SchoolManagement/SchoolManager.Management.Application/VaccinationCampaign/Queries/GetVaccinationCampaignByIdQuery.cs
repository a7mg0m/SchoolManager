using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetVaccinationCampaignByIdQuery(Guid Id) : ICommand<VaccinationCampaignDto?>;
}
