using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteSubjectCommand(Guid Id) : ICommand<bool>;
}
