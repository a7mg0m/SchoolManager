using BuildingBlocks;

namespace SchoolManager.Attendance.Domain;

/// <summary>
/// One trip of a bus (to school or home) on a day, with its students. While it runs, the bus position is updated
/// so parents can follow their child: Planned → InProgress → Completed (or Cancelled).
/// </summary>
public sealed class Trip : BaseEntity
{
    public Guid BusId { get; private set; }
    public DateOnly Date { get; private set; }
    public TripType Type { get; private set; }
    public TripStatus Status { get; private set; }

    /// <summary>UTC</summary>
    public DateTime? StartedAt { get; private set; }

    /// <summary>UTC</summary>
    public DateTime? EndedAt { get; private set; }

    // Last known position of the bus (while the trip runs)
    public double? CurrentLatitude { get; private set; }
    public double? CurrentLongitude { get; private set; }

    /// <summary>When the position was received, UTC</summary>
    public DateTime? LastLocationAt { get; private set; }
    public AuditInfo AuditInfo { get; private set; } = default!;

    private readonly List<TripStudent> _students = new();
    public IReadOnlyList<TripStudent> Students => _students;

    private Trip() { }

    public static Trip Create(Guid busId, DateOnly date, TripType type, AuditInfo auditInfo)
    {
        if (busId == Guid.Empty)
            throw new DomainException("Bus is required.");
        if (!Enum.IsDefined(type))
            throw new DomainException("Trip type is not valid.");

        return new Trip
        {
            BusId = busId,
            Date = date,
            Type = type,
            Status = TripStatus.Planned,
            AuditInfo = auditInfo
        };
    }

    /// <summary>Adds a student to the trip (once; only before or during the trip)</summary>
    public void AddStudent(Guid studentId)
    {
        if (Status is not (TripStatus.Planned or TripStatus.InProgress))
            throw new DomainException("Students can be added only to a planned or running trip.");
        if (studentId == Guid.Empty)
            throw new DomainException("Student is required.");
        if (_students.Any(s => s.StudentId == studentId))
            throw new DomainException("The student is already on this trip.");

        _students.Add(TripStudent.Create(Id, studentId));
    }

    public void Start(DateTime at)
    {
        if (Status != TripStatus.Planned)
            throw new DomainException("Only a planned trip can start.");

        StartedAt = at;
        Status = TripStatus.InProgress;
    }

    public void Complete(DateTime at)
    {
        if (Status != TripStatus.InProgress)
            throw new DomainException("Only a running trip can be completed.");
        if (StartedAt is DateTime started && at < started)
            throw new DomainException("A trip cannot end before it started.");

        EndedAt = at;
        Status = TripStatus.Completed;
    }

    public void Cancel()
    {
        if (Status == TripStatus.Completed)
            throw new DomainException("A completed trip cannot be cancelled.");

        Status = TripStatus.Cancelled;
    }

    /// <summary>The bus's current position (only while the trip runs)</summary>
    public void UpdateLocation(double latitude, double longitude, DateTime at)
    {
        EnsureInProgress();
        if (latitude is < -90 or > 90)
            throw new DomainException("Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180)
            throw new DomainException("Longitude must be between -180 and 180.");

        CurrentLatitude = latitude;
        CurrentLongitude = longitude;
        LastLocationAt = at;
    }

    public void MarkBoarded(Guid studentId, DateTime at)
    {
        EnsureInProgress();
        Student(studentId).MarkBoarded(at);
    }

    public void MarkDroppedOff(Guid studentId, DateTime at)
    {
        EnsureInProgress();
        Student(studentId).MarkDroppedOff(at);
    }

    /// <summary>The student did not come to the bus</summary>
    public void MarkAbsent(Guid studentId)
    {
        EnsureInProgress();
        Student(studentId).MarkAbsent();
    }

    private TripStudent Student(Guid studentId)
        => _students.FirstOrDefault(s => s.StudentId == studentId)
           ?? throw new DomainException("The student is not on this trip.");

    private void EnsureInProgress()
    {
        if (Status != TripStatus.InProgress)
            throw new DomainException("The trip is not running.");
    }
}
