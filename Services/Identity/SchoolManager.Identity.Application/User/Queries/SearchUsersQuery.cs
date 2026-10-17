using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record SearchUsersQuery(string SearchTerm) : ICommand<List<UserDto>>;
}
