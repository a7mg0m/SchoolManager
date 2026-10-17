using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAllParentsQuery() : ICommand<List<ParentDto>>;
}
