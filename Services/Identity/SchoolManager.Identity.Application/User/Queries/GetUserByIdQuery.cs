using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record GetUserByIdQuery(Guid Id) : ICommand<UserDto?>;
}
