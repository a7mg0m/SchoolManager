using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteClinicVisitCommand(Guid Id) : ICommand<bool>;
}
