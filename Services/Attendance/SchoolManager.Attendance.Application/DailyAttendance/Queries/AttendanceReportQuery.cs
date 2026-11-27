using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>One student's school day in the attendance report (no lesson details, only their counts)</summary>
    public record AttendanceReportRowDto(
        Guid StudentId,
        Guid GradeId,
        Guid SectionId,
        Guid AcademicYearId,
        DateOnly Date,
        DailyAttendanceStatus Status,
        int PresentLessons,
        int LateLessons,
        int AbsentLessons,
        int ExcusedLessons);

    /// <summary>
    /// Admin attendance report: every recorded school day between two dates (both included, at most a year), optionally
    /// of one academic year / grade / class.
    /// </summary>
    public record GetAttendanceReportQuery(DateOnly From, DateOnly To, Guid? AcademicYearId, Guid? GradeId, Guid? SectionId)
        : IQuery<List<AttendanceReportRowDto>>;

    public class GetAttendanceReportQueryHandler(IDailyAttendanceRepository days)
        : IQueryHandler<GetAttendanceReportQuery, List<AttendanceReportRowDto>>
    {
        public const int MaxDays = 366;

        public async Task<List<AttendanceReportRowDto>> Handle(GetAttendanceReportQuery query, CancellationToken cancellationToken)
        {
            var from = query.From;
            var to = query.To < from ? from : query.To;
            if (to.DayNumber - from.DayNumber > MaxDays) to = from.AddDays(MaxDays);

            // Counted by SQL: the lessons themselves are not loaded
            var rows = await days.GetReportRowsAsync(from, to, query.AcademicYearId, query.GradeId, query.SectionId, cancellationToken);
            return rows
                .Select(d => new AttendanceReportRowDto(
                    d.StudentId, d.GradeId, d.SectionId, d.AcademicYearId, d.Date, d.Status,
                    d.PresentLessons, d.LateLessons, d.AbsentLessons, d.ExcusedLessons))
                .ToList();
        }
    }
}
