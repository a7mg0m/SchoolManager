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
        private sealed record ChildItem(Guid Id, Guid GradeId, Guid? SectionId, Guid AcademicYearId);
        private sealed record SubjectItem(Guid Id, string? NameAr, string? NameEn);
        private sealed record SectionItem(Guid SectionId, string? NameAr, string? NameEn);
        private sealed record TeacherSubjectItem(Guid SubjectId, string? SubjectNameAr, string? SubjectNameEn, Guid GradeId,
            string? GradeNameAr, string? GradeNameEn, List<SectionItem>? Sections, Guid StageId);
        private sealed record StudentItem(Guid Id, string? NameAr, string? NameEn);
        private sealed record ClassesItem(Guid? AcademicYearId, Guid? SemesterId, List<TeacherSubjectItem>? Subjects);
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

        public async Task<(ParentChild? Student, bool Unavailable)> GetTaughtStudentAsync(string authorization, Guid studentId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await SendAsync($"api/teacher/teaches-student/{studentId}", authorization, cancellationToken);
                if (response is null || (int)response.StatusCode is 401 or 403 or 404)
                    return (null, false); // not taught / not a teacher
                if (!response.IsSuccessStatusCode)
                    return (null, true);

                var body = await response.Content.ReadFromJsonAsync<ApiResponse<ChildItem>>(cancellationToken);
                return body?.Data is ChildItem c
                    ? (new ParentChild(c.Id, c.GradeId, c.SectionId, c.AcademicYearId), false)
                    : (null, false);
            }
            catch (Exception ex) when (IsCommunicationError(ex))
            {
                return (null, true);
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

        public async Task<TeacherClasses?> GetMyClassesAsync(string authorization, Guid? academicYearId = null, Guid? semesterId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await SendAsync(
                    MyClassesPath(academicYearId, semesterId),
                    authorization, cancellationToken);
                if (response is null || (int)response.StatusCode is 401 or 403)
                    return new TeacherClasses(null, null, Array.Empty<TeacherSubject>()); // not a teacher: teaches nothing
                if (!response.IsSuccessStatusCode)
                    return null;

                var body = await response.Content.ReadFromJsonAsync<ApiResponse<ClassesItem>>(cancellationToken);
                var data = body?.Data;
                return new TeacherClasses(
                    data?.AcademicYearId,
                    data?.SemesterId,
                    (data?.Subjects ?? new List<TeacherSubjectItem>())
                        .Select(s => new TeacherSubject(
                            s.SubjectId, s.SubjectNameAr ?? string.Empty, s.SubjectNameEn ?? string.Empty,
                            s.GradeId, s.GradeNameAr ?? string.Empty, s.GradeNameEn ?? string.Empty,
                            (s.Sections ?? new List<SectionItem>())
                                .Select(x => new TeacherSection(x.SectionId, x.NameAr ?? string.Empty, x.NameEn ?? string.Empty))
                                .ToList(),
                            s.StageId))
                        .ToList());
            }
            catch (Exception ex) when (IsCommunicationError(ex))
            {
                return null;
            }
        }

        public async Task<IReadOnlyList<SectionStudent>?> GetMySectionStudentsAsync(string authorization, Guid sectionId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await SendAsync($"api/teacher/my-students/{sectionId}", authorization, cancellationToken);
                if (response is null || (int)response.StatusCode is 401 or 403 or 404)
                    return Array.Empty<SectionStudent>(); // not a teacher / not their class: no students
                if (!response.IsSuccessStatusCode)
                    return null;

                var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<StudentItem>>>(cancellationToken);
                return (body?.Data ?? new List<StudentItem>())
                    .Select(s => new SectionStudent(s.Id, s.NameAr ?? string.Empty, s.NameEn ?? string.Empty))
                    .ToList();
            }
            catch (Exception ex) when (IsCommunicationError(ex))
            {
                return null;
            }
        }

        /// <summary>api/teacher/my-classes with the chosen academic year / semester</summary>
        private static string MyClassesPath(Guid? academicYearId, Guid? semesterId)
        {
            var query = new List<string>();
            if (academicYearId is Guid year) query.Add($"academicYearId={year}");
            if (semesterId is Guid semester) query.Add($"semesterId={semester}");
            return query.Count == 0 ? "api/teacher/my-classes" : "api/teacher/my-classes?" + string.Join("&", query);
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
