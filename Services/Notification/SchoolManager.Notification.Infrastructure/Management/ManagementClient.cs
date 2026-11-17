using System.Net.Http.Headers;
using System.Net.Http.Json;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.Infrastructure
{
    /// <summary>
    /// Calls the Management service through the gateway, forwarding the logged-in user's token,
    /// so Management decides who the parent is from the token; this service never trusts ids sent by the app.
    /// </summary>
    public class ManagementClient(HttpClient httpClient) : IManagementClient
    {
        private sealed record ChildItem(Guid Id, Guid StageId, Guid GradeId, Guid SectionId, Guid AcademicYearId);
        private sealed record ApiResponse<T>(T? Data);

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
