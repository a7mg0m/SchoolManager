using BuildingBlocks.CQRS;

namespace SchoolManager.Attendance.Application
{
    public record GetDailyAttendanceByIdQuery(Guid Id) : IQuery<DailyAttendanceDto?>;

    /// <summary>The days of a section's students on one date</summary>
    public record GetSectionAttendanceByDateQuery(Guid SectionId, DateOnly Date) : IQuery<List<DailyAttendanceDto>>;

    /// <summary>What was recorded for one lesson (timetable slot) on a date, one row per student</summary>
    public record GetPeriodAttendanceQuery(Guid TimetableSlotId, DateOnly Date) : IQuery<List<PeriodAttendanceDto>>;

    public record GetStudentAttendanceQuery(Guid StudentId, DateOnly From, DateOnly To) : IQuery<List<DailyAttendanceDto>>;

    /// <summary>
    /// Parent app: attendance of one of the logged-in parent's children. Null when the Management service
    /// could not tell the parent's children.
    /// </summary>
    /// <param name="Authorization">The parent's "Authorization" header (to ask Management for the children)</param>
    public record GetMyChildAttendanceQuery(Guid StudentId, DateOnly From, DateOnly To, string Authorization) : IQuery<List<DailyAttendanceDto>?>;
}
