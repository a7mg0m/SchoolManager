using System.Net.Http.Headers;
using System.Net.Http.Json;
using SchoolManager.ExamsHomework.Application;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    /// <summary>
    /// Calls the Management service through the gateway, forwarding the logged-in user's token,
    /// so Management decides who the parent is from the token; this service never trusts ids sent by the app.
    /// </summary>
    public class ManagementClient(HttpClient httpClient) : IManagementClient
    {
        private sealed record ChildItem(Guid Id, Guid GradeId, Guid SectionId, Guid AcademicYearId);
        private sealed record SubjectItem(Guid Id, string? NameAr, string? NameEn);
        private sealed record ApiResponse<T>(T? Data);

        public async Task<IReadOnlyList<ParentChild>?> GetMyChildrenAsync(string authorization, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await SendAsync("api/student/my-children", authorization, cancellationToken);
                if (response is null || (int)response.StatusCode is 401 or 403)
                    return Array.Empty<ParentChild>(); // no token / not a parent: no children
                if (!response.IsSuccessStatusCode)
                    return null;

                var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<ChildItem>>>(cancellationToken);
                return body?.Data?.Select(c => new ParentChild(c.Id, c.GradeId, c.SectionId, c.AcademicYearId)).ToList()
                    ?? new List<ParentChild>();
            }
            catch (Exception ex) when (IsCommunicationError(ex))
            {
                return null;
            }
        }

        public async Task<IReadOnlyDictionary<Guid, (string Ar, string En)>> GetSubjectNamesAsync(string authorization, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await SendAsync("api/subject/get-all", authorization, cancellationToken);
                if (response is null || !response.IsSuccessStatusCode)
                    return new Dictionary<Guid, (string, string)>();

                var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SubjectItem>>>(cancellationToken);
                return (body?.Data ?? new List<SubjectItem>())
                    .GroupBy(s => s.Id)
                    .ToDictionary(g => g.Key, g => (g.First().NameAr ?? string.Empty, g.First().NameEn ?? string.Empty));
            }
            catch (Exception ex) when (IsCommunicationError(ex))
            {
                return new Dictionary<Guid, (string, string)>(); // names are optional: the exams still show
            }
        }

        private async Task<HttpResponseMessage?> SendAsync(string path, string authorization, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(authorization) || !AuthenticationHeaderValue.TryParse(authorization, out var header))
                return null;

            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = header;
            return await httpClient.SendAsync(request, cancellationToken);
        }

        private static bool IsCommunicationError(Exception ex)
            => ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException;
    }
}
