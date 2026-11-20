using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteVaccinationCampaignCommand(Guid Id) : ICommand<bool>;
}
