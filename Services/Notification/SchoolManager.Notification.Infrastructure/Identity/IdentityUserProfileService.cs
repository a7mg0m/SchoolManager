using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.Infrastructure
{
    /// <summary>
    /// Resolves display names from the Identity service (GET api/user-profile/names?ids=…, through the gateway, with the
    /// user's token). Names are cached in memory for a while, so the chat screens' polling does not call Identity every
    /// time; only ids not in the cache are asked. When Identity does not answer, the chat still works without names.
    /// </summary>
    public class IdentityUserProfileService(HttpClient httpClient, IMemoryCache cache) : IUserProfileService
    {
        private sealed record UserNameResponse(Guid Id, string FullName);

        /// <summary>How long a name is kept (a renamed user shows the new name after this)</summary>
        private static readonly TimeSpan NameLifetime = TimeSpan.FromMinutes(10);

        /// <summary>A user Identity does not know (or a failed call) is not asked again for this long</summary>
        private static readonly TimeSpan MissLifetime = TimeSpan.FromMinutes(1);

        private static string Key(Guid id) => $"chat-user-name:{id}";

        public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
        {
            var ids = userIds.Where(id => id != Guid.Empty).Distinct().ToList();
            var names = new Dictionary<Guid, string>();
            var missing = new List<Guid>();

            foreach (var id in ids)
            {
                if (cache.TryGetValue(Key(id), out string? name))
                {
                    if (!string.IsNullOrEmpty(name)) names[id] = name;
                }
                else
                {
                    missing.Add(id);
                }
            }
            if (missing.Count == 0) return names;

            List<UserNameResponse>? users = null;
            try
            {
                var url = "api/user-profile/names?" + string.Join("&", missing.Select(id => $"ids={id}"));
                users = await httpClient.GetFromJsonAsync<List<UserNameResponse>>(url, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or NotSupportedException)
            {
                // Identity unavailable: no names this time (and not asked again for a minute)
            }

            var found = (users ?? new List<UserNameResponse>()).ToDictionary(u => u.Id, u => u.FullName);
            foreach (var id in missing)
            {
                if (found.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name))
                {
                    names[id] = name;
                    cache.Set(Key(id), name, NameLifetime);
                }
                else
                {
                    cache.Set(Key(id), string.Empty, MissLifetime);
                }
            }
            return names;
        }
    }
}
