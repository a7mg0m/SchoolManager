using System.Net.Http.Headers;
using System.Net.Http.Json;
using SchoolManager.Attendance.Application;

namespace SchoolManager.Attendance.Infrastructure
{
    /// <summary>
    /// Asks the Management service (POST api/student/bus-riders, through the gateway) for the names, classes and
    /// parents' phones of the students on a trip. The driver's token is forwarded; Management only answers drivers.
    /// </summary>
    public class ManagementBusRiderDirectory(HttpClient httpClient) : IBusRiderDirectory
    {
        private sealed record RidersResponse(List<BusRiderInfo>? Data);

        public async Task<IReadOnlyList<BusRiderInfo>?> GetRidersAsync(
            IReadOnlyCollection<Guid> studentIds, string authorization, CancellationToken cancellationToken = default)
        {
            if (studentIds.Count == 0)
                return Array.Empty<BusRiderInfo>();
            if (string.IsNullOrWhiteSpace(authorization) || !AuthenticationHeaderValue.TryParse(authorization, out var header))
                return null;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/student/bus-riders")
                {
                    Content = JsonContent.Create(studentIds)
                };
                request.Headers.Authorization = header;

                using var response = await httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return null;

                var body = await response.Content.ReadFromJsonAsync<RidersResponse>(cancellationToken);
                return body?.Data ?? new List<BusRiderInfo>();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                return null;
            }
        }
    }
}
