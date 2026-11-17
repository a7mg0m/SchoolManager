using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>Active children of a parent (the controller passes the parent id from the login token).</summary>
    public record GetMyChildrenQuery(Guid ParentId) : ICommand<List<ChildDto>>;
}
