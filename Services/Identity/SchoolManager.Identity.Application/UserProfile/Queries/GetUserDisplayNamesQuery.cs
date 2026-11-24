using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>Display names (first + last name of the profile) of some users, for the chat (Notification service)</summary>
    public record GetUserDisplayNamesQuery(IReadOnlyCollection<Guid> UserIds) : IQuery<List<UserDisplayNameDto>>;

    public record UserDisplayNameDto(Guid Id, string FullName);

    public class GetUserDisplayNamesQueryHandler(IUserProfileRepository profiles)
        : IQueryHandler<GetUserDisplayNamesQuery, List<UserDisplayNameDto>>
    {
        public async Task<List<UserDisplayNameDto>> Handle(GetUserDisplayNamesQuery query, CancellationToken cancellationToken)
        {
            var ids = query.UserIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (ids.Count == 0) return new List<UserDisplayNameDto>();

            var found = await profiles.SearchAsync(p => ids.Contains(p.UserId));
            return found
                .Select(p => new UserDisplayNameDto(p.UserId, $"{p.FirstName} {p.LastName}".Trim()))
                .ToList();
        }
    }
}
