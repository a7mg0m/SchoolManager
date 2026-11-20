using BuildingBlocks;

namespace SchoolManager.Attendance.Domain;

/// <summary>
/// Attendance of a student in one lesson (timetable slot) of the day. Created and changed only through
/// <see cref="DailyAttendance.RecordPeriod"/>.
/// </summary>
public sealed class PeriodAttendance : BaseEntity
{
    public const int MaxAbsenceReasonLength = 500;

    public Guid DailyAttendanceId { get; private set; }

    // Owned by the Management service (no navigation)
    public Guid TimetableSlotId { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid TeacherId { get; private set; }

    /// <summary>Start of the lesson</summary>
    public TimeOnly StartTime { get; private set; }
    public PeriodAttendanceStatus Status { get; private set; }
    public string? AbsenceReason { get; private set; }

    /// <summary>When the teacher recorded (or last changed) it, UTC</summary>
    public DateTime RecordedAt { get; private set; }
    public AuditInfo AuditInfo { get; private set; } = default!;

    private PeriodAttendance() { }

    internal static PeriodAttendance Create(
        Guid dailyAttendanceId, Guid timetableSlotId, Guid subjectId, Guid teacherId, TimeOnly startTime,
        PeriodAttendanceStatus status, string? absenceReason, AuditInfo auditInfo)
    {
        if (timetableSlotId == Guid.Empty)
            throw new DomainException("Timetable slot is required.");
        if (subjectId == Guid.Empty)
            throw new DomainException("Subject is required.");
        if (teacherId == Guid.Empty)
            throw new DomainException("Teacher is required.");

        var period = new PeriodAttendance
        {
            DailyAttendanceId = dailyAttendanceId,
            TimetableSlotId = timetableSlotId,
            SubjectId = subjectId,
            TeacherId = teacherId,
            StartTime = startTime
        };
        period.Update(status, absenceReason, auditInfo);
        return period;
    }

    internal void Update(PeriodAttendanceStatus status, string? absenceReason, AuditInfo auditInfo)
    {
        if (!Enum.IsDefined(status))
            throw new DomainException("Attendance status is not valid.");

        absenceReason = string.IsNullOrWhiteSpace(absenceReason) ? null : absenceReason.Trim();
        if (absenceReason?.Length > MaxAbsenceReasonLength)
            throw new DomainException($"Absence reason cannot exceed {MaxAbsenceReasonLength} characters.");

        Status = status;
        // A reason only for a lesson the student missed
        AbsenceReason = status is PeriodAttendanceStatus.Absent or PeriodAttendanceStatus.Excused ? absenceReason : null;
        RecordedAt = DateTime.UtcNow;
        AuditInfo = auditInfo;
    }
}
