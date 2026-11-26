namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// Does the logged-in teacher teach a student? From the Management service (GET api/teacher/teaches-student/{id}),
    /// asked with the teacher's own token — Attendance does not know who teaches which class.
    /// </summary>
    public interface ITeacherStudentsService
    {
        /// <summary>True / false; null when the Management service could not answer.</summary>
        Task<bool?> TeachesStudentAsync(string authorization, Guid studentId, CancellationToken cancellationToken = default);
    }
}
