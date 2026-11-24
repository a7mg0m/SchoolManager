using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.Infrastructure
{
    /// <summary>
    /// Calls the Management service through the gateway, forwarding the logged-in user's token,
    /// so Management decides who the parent is from the token; this service never trusts ids sent by the app.
    /// </summary>
    public class ManagementClient(HttpClient httpClient, IMemoryCache cache, ServiceTokenProvider serviceToken) : IManagementClient
    {
        private sealed record ChildItem(Guid Id, Guid StageId, Guid GradeId, Guid SectionId, Guid AcademicYearId);
        private sealed record ApiResponse<T>(T? Data);
        private sealed record NamedItem(string? NameAr, string? NameEn);
        private sealed record ParentItem(Guid Id, string? NameAr, string? NameEn, List<NamedItem>? Children);
        private sealed record TeacherItem(Guid TeacherId, string? NameAr, string? NameEn, List<NamedItem>? Subjects);

        /// <summary>
        /// Contacts change rarely (a new class or child), while the chat list is polled every few seconds:
        /// the answer is kept for this long per user (the key is a hash of the user's token)
        /// </summary>
        private static readonly TimeSpan ContactsLifetime = TimeSpan.FromMinutes(2);

        public async Task<IReadOnlyList<Guid>?> GetParentIdsAsync(
            Guid? academicYearId,
            IReadOnlyCollection<Guid> gradeIds,
            IReadOnlyCollection<Guid> sectionIds,
            IReadOnlyCollection<Guid> studentIds,
            CancellationToken cancellationToken = default)
        {
            if (gradeIds.Count + sectionIds.Count + studentIds.Count == 0)
                return Array.Empty<Guid>();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/student/notification-parents")
                {
                    Content = JsonContent.Create(new { academicYearId, gradeIds, sectionIds, studentIds })
                };
                // Our own token: also works from the background push job (no user request)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken.GetToken());

                using var response = await httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return null;

                var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<Guid>>>(cancellationToken);
                return body?.Data ?? new List<Guid>();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                return null;
            }
        }

        public async Task<IReadOnlyList<ChatContact>?> GetChatContactsAsync(string authorization, bool asTeacher, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(authorization))
                return Array.Empty<ChatContact>();

            var key = "chat-contacts:" + (asTeacher ? "t:" : "p:")
                + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(authorization)));
            if (cache.TryGetValue(key, out IReadOnlyList<ChatContact>? cached) && cached is not null)
                return cached;

            var contacts = await LoadChatContactsAsync(authorization, asTeacher, cancellationToken);
            // Only real answers are kept (null = Management unavailable: asked again next time)
            if (contacts is not null)
                cache.Set(key, contacts, ContactsLifetime);
            return contacts;
        }

        private async Task<IReadOnlyList<ChatContact>?> LoadChatContactsAsync(string authorization, bool asTeacher, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(authorization) || !AuthenticationHeaderValue.TryParse(authorization, out var header))
                return Array.Empty<ChatContact>();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, asTeacher ? "api/teacher/my-parents" : "api/student/my-teachers");
                request.Headers.Authorization = header;

                using var response = await httpClient.SendAsync(request, cancellationToken);
                if ((int)response.StatusCode is 401 or 403)
                    return Array.Empty<ChatContact>();
                if (!response.IsSuccessStatusCode)
                    return null;

                static string Join(IEnumerable<NamedItem>? items, Func<NamedItem, string?> pick)
                    => string.Join("، ", (items ?? Enumerable.Empty<NamedItem>()).Select(pick).Where(n => !string.IsNullOrWhiteSpace(n)));

                if (asTeacher)
                {
                    var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<ParentItem>>>(cancellationToken);
                    return (body?.Data ?? new List<ParentItem>())
                        .Select(p => new ChatContact(p.Id, p.NameAr ?? string.Empty, p.NameEn ?? string.Empty,
                            "ولي أمر " + Join(p.Children, c => c.NameAr),
                            "Parent of " + Join(p.Children, c => string.IsNullOrWhiteSpace(c.NameEn) ? c.NameAr : c.NameEn).Replace("، ", ", ")))
                        .ToList();
                }

                var teachers = await response.Content.ReadFromJsonAsync<ApiResponse<List<TeacherItem>>>(cancellationToken);
                return (teachers?.Data ?? new List<TeacherItem>())
                    .Select(t => new ChatContact(t.TeacherId, t.NameAr ?? string.Empty, t.NameEn ?? string.Empty,
                        Join(t.Subjects, s => s.NameAr),
                        Join(t.Subjects, s => string.IsNullOrWhiteSpace(s.NameEn) ? s.NameAr : s.NameEn).Replace("، ", ", ")))
                    .ToList();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                return null;
            }
        }

        public async Task<IReadOnlyList<ParentChild>?> GetMyChildrenAsync(string authorization, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(authorization) || !AuthenticationHeaderValue.TryParse(authorization, out var header))
                return Array.Empty<ParentChild>();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "api/student/my-children");
                request.Headers.Authorization = header;

                using var response = await httpClient.SendAsync(request, cancellationToken);
                if ((int)response.StatusCode is 401 or 403)
                    return Array.Empty<ParentChild>(); // not a parent: no children
                if (!response.IsSuccessStatusCode)
                    return null;

                var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<ChildItem>>>(cancellationToken);
                return body?.Data?
                    .Select(c => new ParentChild(c.Id, c.StageId, c.GradeId, c.SectionId, c.AcademicYearId))
                    .ToList() ?? new List<ParentChild>();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                return null;
            }
        }
    }
}
