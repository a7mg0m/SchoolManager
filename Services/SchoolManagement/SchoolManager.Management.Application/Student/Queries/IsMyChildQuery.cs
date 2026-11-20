using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>True when the student exists and belongs to the parent (parent app ownership check)</summary>
    public record IsMyChildQuery(Guid StudentId, Guid ParentId) : IQuery<bool>;
}
