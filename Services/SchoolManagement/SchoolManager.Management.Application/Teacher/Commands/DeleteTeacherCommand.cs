using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteTeacherCommand(Guid Id) : ICommand<bool>;
}
