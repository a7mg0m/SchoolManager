using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteStudentCommand(Guid Id) : ICommand<bool>;
}
