using BuildingBlocks;

namespace SchoolManager.Attendance.Domain
{
    /// <summary>A student's school day with the number of lessons in each state (attendance report)</summary>
    public sealed record DailyAttendanceCounts(
        Guid StudentId, Guid GradeId, Guid SectionId, Guid AcademicYearId, DateOnly Date, DailyAttendanceStatus Status,
        int PresentLessons, int LateLessons, int AbsentLessons, int ExcusedLessons);

    public interface IDailyAttendanceRepository : IRepository<DailyAttendance>
    {
        /// <summary>The recorded days between two dates (optionally of a year / grade / class), lessons counted by SQL</summary>
        Task<List<DailyAttendanceCounts>> GetReportRowsAsync(DateOnly from, DateOnly to, Guid? academicYearId, Guid? gradeId,
            Guid? sectionId, CancellationToken cancellationToken = default);
    }
}
