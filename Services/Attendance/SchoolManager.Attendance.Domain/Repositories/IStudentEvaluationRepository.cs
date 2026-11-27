using BuildingBlocks;

namespace SchoolManager.Attendance.Domain
{
    public interface IStudentEvaluationRepository : IRepository<StudentEvaluation>
    {
        /// <summary>The evaluations between two dates, newest first (read-only)</summary>
        Task<List<StudentEvaluation>> GetBetweenAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    }
}
