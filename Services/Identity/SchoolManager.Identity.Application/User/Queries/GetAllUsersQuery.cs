
using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record GetAllUsersQuery() : ICommand<List<UserDto>>;
}
