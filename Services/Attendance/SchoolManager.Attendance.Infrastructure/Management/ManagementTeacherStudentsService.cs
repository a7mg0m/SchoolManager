using System.Net;
using System.Net.Http.Headers;
using SchoolManager.Attendance.Application;

namespace SchoolManager.Attendance.Infrastructure
{
    /// <summary>
    /// Asks the Management service (through the gateway) whether the logged-in teacher teaches a student. The teacher's
    /// token is forwarded, so Management decides from its teacher_id claim.
    /// </summary>
    public class ManagementTeacherStudentsService(HttpClient httpClient) : ITeacherStudentsService
    {
        public async Task<bool?> TeachesStudentAsync(string authorization, Guid studentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(authorization) || !AuthenticationHeaderValue.TryParse(authorization, out var header))
                return false;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"api/teacher/teaches-student/{studentId}");
                request.Headers.Authorization = header;

                using var response = await httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode) return true;
                // Not taught / not a teacher / token rejected
                if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
                    return false;
                return null;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return null;
            }
        }
    }
}
