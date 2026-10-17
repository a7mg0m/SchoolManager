using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetParentByIdQuery(Guid Id) : ICommand<ParentDto?>;
}
