using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record GetAllRolesQuery() : ICommand<List<RoleDto>>;
}
