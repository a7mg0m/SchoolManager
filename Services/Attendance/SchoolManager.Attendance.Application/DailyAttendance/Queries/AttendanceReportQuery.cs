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

            var year = query.AcademicYearId;
            var grade = query.GradeId;
            var section = query.SectionId;
            var list = await days.SearchAsync(d =>
                d.Date >= from && d.Date <= to
                && (year == null || d.AcademicYearId == year)
                && (grade == null || d.GradeId == grade)
                && (section == null || d.SectionId == section));

            return list
                .OrderBy(d => d.Date)
                .Select(d => new AttendanceReportRowDto(
                    d.StudentId, d.GradeId, d.SectionId, d.AcademicYearId, d.Date, d.Status,
                    d.Periods.Count(p => p.Status == PeriodAttendanceStatus.Present),
                    d.Periods.Count(p => p.Status == PeriodAttendanceStatus.Late),
                    d.Periods.Count(p => p.Status == PeriodAttendanceStatus.Absent),
                    d.Periods.Count(p => p.Status == PeriodAttendanceStatus.Excused)))
                .ToList();
        }
    }
}
