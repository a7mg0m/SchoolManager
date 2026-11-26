using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SchoolManager.Identity.Application;

namespace SchoolManager.Identity.Infrastructure
{
    /// <summary>
    /// GET api/parent/has-current-students/{parentId} on the Management service (through the gateway), with a short-lived
    /// service token signed with the same Jwt settings (no parent / teacher / driver claim, "svc" = "identity").
    /// </summary>
    public class ManagementParentStudentsChecker(HttpClient httpClient, IConfiguration configuration, ILogger<ManagementParentStudentsChecker> logger)
        : IParentStudentsChecker
    {
        private sealed record BoolResponse(bool Data);

        public async Task<bool?> HasCurrentStudentsAsync(Guid parentId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"api/parent/has-current-students/{parentId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagementServiceToken.Create(configuration));

                using var response = await httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Management answered {Status} for parent {ParentId}'s current students", (int)response.StatusCode, parentId);
                    return null;
                }
                var body = await response.Content.ReadFromJsonAsync<BoolResponse>(cancellationToken);
                return body?.Data;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                logger.LogWarning(ex, "Could not ask Management about parent {ParentId}'s current students", parentId);
                return null;
            }
        }

    }
}
