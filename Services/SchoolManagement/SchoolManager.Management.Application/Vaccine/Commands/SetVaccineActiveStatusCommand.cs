using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record SetVaccineActiveStatusCommand(Guid Id, bool IsActive) : ICommand<bool>;
}
