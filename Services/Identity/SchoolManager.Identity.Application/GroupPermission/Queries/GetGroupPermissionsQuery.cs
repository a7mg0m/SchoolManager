using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    /// <summary>The whole menu tree with the permissions of one group. Null when the group does not exist.</summary>
    public record GetGroupPermissionsQuery(Guid EmployeeGroupId) : IQuery<GroupPermissionsDto?>;
}
