using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteSchoolInfoCommand(Guid Id) : ICommand<bool>;
}
