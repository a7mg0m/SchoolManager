using System.Net.Http.Json;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.Infrastructure
{
    /// <summary>
    /// Resolves display names from the Identity Service. Names are fetched on demand and never persisted.
    /// Expected contract: GET {IdentityService:BaseUrl}/api/users/names?ids={id}&amp;ids={id} returns [{ "id": guid, "fullName": string }].
    /// </summary>
    public class IdentityUserProfileService(HttpClient httpClient) : IUserProfileService
    {
        private sealed record UserNameResponse(Guid Id, string FullName);

        public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
        {
            var ids = userIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<Guid, string>();

            try
            {
                var url = "api/users/names?" + string.Join("&", ids.Select(id => $"ids={id}"));
                var users = await httpClient.GetFromJsonAsync<List<UserNameResponse>>(url, cancellationToken);
                return users?.ToDictionary(u => u.Id, u => u.FullName) ?? new Dictionary<Guid, string>();
            }
            catch (HttpRequestException)
            {
                return new Dictionary<Guid, string>();
            }
        }
    }
}
