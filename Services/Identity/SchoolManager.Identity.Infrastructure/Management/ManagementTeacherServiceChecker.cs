using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchoolManager.Identity.Application;

namespace SchoolManager.Identity.Infrastructure
{
    /// <summary>GET api/teacher/in-service/{teacherId} on the Management service (through the gateway), with a service token.</summary>
    public class ManagementTeacherServiceChecker(HttpClient httpClient, IConfiguration configuration, ILogger<ManagementTeacherServiceChecker> logger)
        : ITeacherServiceChecker
    {
        private sealed record BoolResponse(bool Data);

        public async Task<bool?> IsInServiceAsync(Guid teacherId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"api/teacher/in-service/{teacherId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagementServiceToken.Create(configuration));

                using var response = await httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Management answered {Status} for teacher {TeacherId}'s service", (int)response.StatusCode, teacherId);
                    return null;
                }
                var body = await response.Content.ReadFromJsonAsync<BoolResponse>(cancellationToken);
                return body?.Data;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                logger.LogWarning(ex, "Could not ask Management about teacher {TeacherId}'s service", teacherId);
                return null;
            }
        }
    }
}
