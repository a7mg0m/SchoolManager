using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record SetVaccinationCampaignActiveStatusCommand(Guid Id, bool IsActive) : ICommand<bool>;
}
