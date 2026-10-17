using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteGradeCommand(Guid Id) : ICommand<bool>;
}
