using System.Net.Http.Headers;
using System.Net.Http.Json;
using SchoolManager.Attendance.Application;

namespace SchoolManager.Attendance.Infrastructure
{
    /// <summary>
    /// Asks the Management service (GET api/student/my-children, through the gateway) for the
    /// logged-in parent's children. The parent's token is forwarded, so Management decides from the
    /// token's parent_id claim; Attendance never trusts a parent id sent by the app.
    /// </summary>
    public class ManagementParentChildrenService(HttpClient httpClient) : IParentChildrenService
    {
        private sealed record ChildItem(Guid Id, string? FullNameAr, string? FullNameEn, Guid AcademicYearId);
        private sealed record ChildrenResponse(List<ChildItem>? Data);

        public async Task<IReadOnlyCollection<Guid>?> GetMyChildrenIdsAsync(string authorization, CancellationToken cancellationToken = default)
            => (await GetMyChildrenAsync(authorization, cancellationToken))?.Select(c => c.Id).ToList();

        public async Task<IReadOnlyCollection<ParentChildInfo>?> GetMyChildrenAsync(string authorization, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(authorization) || !AuthenticationHeaderValue.TryParse(authorization, out var header))
                return Array.Empty<ParentChildInfo>();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "api/student/my-children");
                request.Headers.Authorization = header;

                using var response = await httpClient.SendAsync(request, cancellationToken);
                // Not a parent / token rejected: no children
                if ((int)response.StatusCode is 401 or 403)
                    return Array.Empty<ParentChildInfo>();
                if (!response.IsSuccessStatusCode)
                    return null;

                var body = await response.Content.ReadFromJsonAsync<ChildrenResponse>(cancellationToken);
                return body?.Data?.Select(c => new ParentChildInfo(c.Id, c.FullNameAr ?? "", c.FullNameEn ?? "", c.AcademicYearId)).ToList()
                    ?? new List<ParentChildInfo>();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                return null;
            }
        }
    }
}
