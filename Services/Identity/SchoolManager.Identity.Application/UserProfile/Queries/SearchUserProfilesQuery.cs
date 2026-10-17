using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record SearchUserProfilesQuery(string SearchTerm) : ICommand<List<UserProfileDto>>;
}
