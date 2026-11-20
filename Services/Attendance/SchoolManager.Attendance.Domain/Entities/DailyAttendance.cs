using BuildingBlocks;

namespace SchoolManager.Attendance.Domain;

/// <summary>
/// Attendance of one student for one school day (one row per student per day), with the lessons (periods)
/// recorded by the teachers. The day's status follows the periods; the school office can also set it.
/// </summary>
public sealed class DailyAttendance : BaseEntity
{
    public const int MaxNotesLength = 500;

    // Owned by other services (no navigation)
    public Guid StudentId { get; private set; }
    public Guid SectionId { get; private set; }
    public Guid GradeId { get; private set; }
    public Guid AcademicYearId { get; private set; }

    public DateOnly Date { get; private set; }
    public DailyAttendanceStatus Status { get; private set; }

    /// <summary>Who recorded the day last: a teacher (periods) or the school office (admin dashboard)</summary>
    public AttendanceSource Source { get; private set; }

    /// <summary>Start of the first lesson the student attended (or the time set by the office)</summary>
    public TimeOnly? CheckInTime { get; private set; }
    public TimeOnly? CheckOutTime { get; private set; }
    public string? Notes { get; private set; }
    public AuditInfo AuditInfo { get; private set; } = default!;

    private readonly List<PeriodAttendance> _periods = new();
    public IReadOnlyList<PeriodAttendance> Periods => _periods;

    // Derived values (not stored)
    /// <summary>Lessons attended (present or late)</summary>
    public int AttendedPeriodsCount => _periods.Count(p => p.Status is PeriodAttendanceStatus.Present or PeriodAttendanceStatus.Late);
    public int AbsentPeriodsCount => _periods.Count(p => p.Status == PeriodAttendanceStatus.Absent);

    private DailyAttendance() { }

    /// <summary>
    /// Starts the student's day: when the first lesson of the day is recorded (teacher), or by the office (admin).
    /// </summary>
    public static DailyAttendance Create(
        Guid studentId, Guid sectionId, Guid gradeId, Guid academicYearId, DateOnly date,
        AttendanceSource source, AuditInfo auditInfo,
        DailyAttendanceStatus status = DailyAttendanceStatus.Present, string? notes = null)
    {
        if (studentId == Guid.Empty)
            throw new DomainException("Student is required.");
        if (sectionId == Guid.Empty)
            throw new DomainException("Section is required.");
        if (gradeId == Guid.Empty)
            throw new DomainException("Grade is required.");
        if (academicYearId == Guid.Empty)
            throw new DomainException("Academic year is required.");

        return new DailyAttendance
        {
            StudentId = studentId,
            SectionId = sectionId,
            GradeId = gradeId,
            AcademicYearId = academicYearId,
            Date = date,
            Source = source,
            Status = status,
            Notes = CleanNotes(notes),
            AuditInfo = auditInfo
        };
    }

    /// <summary>
    /// Records the student's attendance in one lesson of the day. A lesson already recorded (same timetable slot)
    /// is updated, never duplicated. The first lesson attended sets the check-in time; the day's status follows.
    /// </summary>
    public PeriodAttendance RecordPeriod(
        Guid timetableSlotId, Guid subjectId, Guid teacherId, TimeOnly startTime,
        PeriodAttendanceStatus status, AuditInfo auditInfo, string? absenceReason = null)
    {
        var period = _periods.FirstOrDefault(p => p.TimetableSlotId == timetableSlotId);
        if (period is null)
        {
            // Copy: AuditInfo is an owned type tracked by reference (shared instances are saved for one row only)
            period = PeriodAttendance.Create(Id, timetableSlotId, subjectId, teacherId, startTime, status, absenceReason, auditInfo.Copy());
            _periods.Add(period);
        }
        else
        {
            period.Update(status, absenceReason, auditInfo.Copy());
        }

        // The first lesson attended is the check-in
        if (period.Status is PeriodAttendanceStatus.Present or PeriodAttendanceStatus.Late
            && (CheckInTime is null || startTime < CheckInTime))
            CheckInTime = startTime;

        Source = AttendanceSource.Teacher;
        AuditInfo = auditInfo;
        RecalculateStatus();
        return period;
    }

    /// <summary>The student left school (e.g. picked up by a parent)</summary>
    public void CheckOut(TimeOnly time, AuditInfo auditInfo)
    {
        if (CheckInTime is TimeOnly checkIn && time < checkIn)
            throw new DomainException("Check-out time cannot be before the check-in time.");

        CheckOutTime = time;
        AuditInfo = auditInfo;
    }

    /// <summary>
    /// The school office sets the day's status from the admin dashboard (e.g. an excused absence).
    /// Recording a lesson later recalculates the status from the lessons.
    /// </summary>
    public void SetDayStatus(DailyAttendanceStatus status, string? notes, AuditInfo auditInfo)
    {
        if (!Enum.IsDefined(status))
            throw new DomainException("Attendance status is not valid.");

        Status = status;
        Notes = CleanNotes(notes);
        Source = AttendanceSource.Admin;
        AuditInfo = auditInfo;
    }

    /// <summary>
    /// The day from its lessons: all attended (present / late) → Present; all excused → Excused;
    /// none attended (absent, or absent and excused) → Absent; some attended and some not → PartiallyPresent
    /// (e.g. attended the first two lessons, then got sick and missed the rest).
    /// </summary>
    private void RecalculateStatus()
    {
        if (_periods.Count == 0)
            return;

        var attended = AttendedPeriodsCount;
        var excused = _periods.Count(p => p.Status == PeriodAttendanceStatus.Excused);

        Status = attended == _periods.Count ? DailyAttendanceStatus.Present
            : excused == _periods.Count ? DailyAttendanceStatus.Excused
            : attended == 0 ? DailyAttendanceStatus.Absent
            : DailyAttendanceStatus.PartiallyPresent;
    }

    private static string? CleanNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return null;

        notes = notes.Trim();
        if (notes.Length > MaxNotesLength)
            throw new DomainException($"Notes cannot exceed {MaxNotesLength} characters.");
        return notes;
    }
}
