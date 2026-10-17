using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record GetAllUserProfilesQuery() : ICommand<List<UserProfileDto>>;
}
