using BuildingBlocks;

namespace SchoolManager.Attendance.Domain;

/// <summary>A student on a bus trip: Pending → Boarded → DroppedOff, or Absent. Changed only through <see cref="Trip"/>.</summary>
public sealed class TripStudent : BaseEntity
{
    public Guid TripId { get; private set; }

    // Owned by the Management service (no navigation)
    public Guid StudentId { get; private set; }

    public TripStudentStatus Status { get; private set; }

    /// <summary>UTC</summary>
    public DateTime? BoardedAt { get; private set; }

    /// <summary>UTC</summary>
    public DateTime? DroppedOffAt { get; private set; }

    private TripStudent() { }

    internal static TripStudent Create(Guid tripId, Guid studentId)
        => new() { TripId = tripId, StudentId = studentId, Status = TripStudentStatus.Pending };

    internal void MarkBoarded(DateTime at)
    {
        if (Status != TripStudentStatus.Pending)
            throw new DomainException("Only a waiting student can board.");

        BoardedAt = at;
        Status = TripStudentStatus.Boarded;
    }

    internal void MarkDroppedOff(DateTime at)
    {
        if (Status != TripStudentStatus.Boarded)
            throw new DomainException("Only a student on the bus can be dropped off.");
        if (BoardedAt is DateTime boarded && at < boarded)
            throw new DomainException("Drop-off cannot be before boarding.");

        DroppedOffAt = at;
        Status = TripStudentStatus.DroppedOff;
    }

    internal void MarkAbsent()
    {
        if (Status != TripStudentStatus.Pending)
            throw new DomainException("Only a waiting student can be marked absent.");

        Status = TripStudentStatus.Absent;
    }
}
