using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record GetUserProfileByIdQuery(Guid Id) : ICommand<UserProfileDto?>;
}
