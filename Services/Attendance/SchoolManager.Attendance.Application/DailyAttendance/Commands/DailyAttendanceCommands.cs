using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// The logged-in teacher records one lesson (timetable slot) for the students of a section.
    /// Each student's day is created with the first lesson recorded; sending the same slot again updates it.
    /// </summary>
    public record RecordPeriodAttendanceCommand(
        Guid TimetableSlotId,
        Guid SubjectId,
        Guid SectionId,
        Guid GradeId,
        Guid AcademicYearId,
        DateOnly Date,
        TimeOnly StartTime,
        List<StudentPeriodStatusDto> Students
    ) : ICommand<bool>;

    /// <summary>
    /// The school office (admin dashboard employee) creates or updates a student's day.
    /// <para>CheckInTime is not applied: the Domain sets it only from the first lesson attended (RecordPeriod).</para>
    /// </summary>
    public record SetDailyAttendanceCommand(
        Guid StudentId,
        Guid SectionId,
        Guid GradeId,
        Guid AcademicYearId,
        DateOnly Date,
        DailyAttendanceStatus Status,
        TimeOnly? CheckInTime,
        TimeOnly? CheckOutTime,
        string? Notes
    ) : ICommand<Guid>;

    /// <summary>The student left school at <paramref name="Time"/> (not before the check-in time)</summary>
    public record CheckOutStudentCommand(Guid Id, TimeOnly Time) : ICommand<bool>;

    public record DeleteDailyAttendanceCommand(Guid Id) : ICommand<bool>;
}
