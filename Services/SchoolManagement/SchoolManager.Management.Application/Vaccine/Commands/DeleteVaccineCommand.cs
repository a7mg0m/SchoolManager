using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteVaccineCommand(Guid Id) : ICommand<bool>;
}
