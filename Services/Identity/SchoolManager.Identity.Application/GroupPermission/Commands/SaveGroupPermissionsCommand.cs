using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Replaces the permissions of an employee group with the given list.
    /// Returns null when the group does not exist.
    /// </summary>
    public record SaveGroupPermissionsCommand(
        Guid EmployeeGroupId,
        List<GroupPermissionDto> Permissions
    ) : ICommand<SaveGroupPermissionsSummary?>;
}
