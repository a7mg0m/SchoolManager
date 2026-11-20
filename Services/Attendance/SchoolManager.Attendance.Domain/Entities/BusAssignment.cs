using BuildingBlocks;

namespace SchoolManager.Attendance.Domain;

/// <summary>
/// A student rides a bus in an academic year, with the student's own bus fee.
/// <para>
/// The bus fee is per student, NOT taken from the grade fees of the academic year (Financial Fee), because one
/// bus carries students of different grades. It will be sent to the Financial service later as a Transportation
/// StudentFee (<see cref="StudentFeeId"/> then links to it).
/// </para>
/// </summary>
public sealed class BusAssignment : BaseEntity
{
    // Owned by other services (no navigation)
    public Guid StudentId { get; private set; }
    public Guid AcademicYearId { get; private set; }

    public Guid BusId { get; private set; }

    /// <summary>Where the student gets on / off; null = not set</summary>
    public Guid? BusRouteStopId { get; private set; }

    /// <summary>The student's bus fee (greater than zero)</summary>
    public Money Fee { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }

    /// <summary>Last day on the bus; null while the assignment runs</summary>
    public DateOnly? EndDate { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>The Transportation StudentFee created for this assignment in the Financial service</summary>
    public Guid? StudentFeeId { get; private set; }
    public AuditInfo AuditInfo { get; private set; } = default!;

    private BusAssignment() { }

    public static BusAssignment Create(
        Guid studentId, Guid busId, Guid academicYearId, Guid? busRouteStopId, Money fee, DateOnly startDate, AuditInfo auditInfo)
    {
        if (studentId == Guid.Empty)
            throw new DomainException("Student is required.");
        if (busId == Guid.Empty)
            throw new DomainException("Bus is required.");
        if (academicYearId == Guid.Empty)
            throw new DomainException("Academic year is required.");
        CheckFee(fee);

        return new BusAssignment
        {
            StudentId = studentId,
            BusId = busId,
            AcademicYearId = academicYearId,
            BusRouteStopId = busRouteStopId == Guid.Empty ? null : busRouteStopId,
            // Copy: Money is an owned type tracked by reference (a shared instance is saved for one row only)
            Fee = new Money(fee.Amount, fee.Currency),
            StartDate = startDate,
            IsActive = true,
            AuditInfo = auditInfo
        };
    }

    public void ChangeFee(Money fee, AuditInfo auditInfo)
    {
        CheckFee(fee);
        Fee = new Money(fee.Amount, fee.Currency);
        AuditInfo = auditInfo;
    }

    /// <summary>The student moves to another bus (and stop)</summary>
    public void MoveToBus(Guid busId, Guid? busRouteStopId, AuditInfo auditInfo)
    {
        if (!IsActive)
            throw new DomainException("The assignment has ended.");
        if (busId == Guid.Empty)
            throw new DomainException("Bus is required.");

        BusId = busId;
        BusRouteStopId = busRouteStopId == Guid.Empty ? null : busRouteStopId;
        AuditInfo = auditInfo;
    }

    /// <summary>The student stops riding the bus after <paramref name="endDate"/></summary>
    public void End(DateOnly endDate, AuditInfo auditInfo)
    {
        if (!IsActive)
            throw new DomainException("The assignment has already ended.");
        if (endDate < StartDate)
            throw new DomainException("End date cannot be before the start date.");

        EndDate = endDate;
        IsActive = false;
        AuditInfo = auditInfo;
    }

    /// <summary>Links the Transportation StudentFee created in the Financial service</summary>
    public void LinkStudentFee(Guid studentFeeId)
    {
        if (studentFeeId == Guid.Empty)
            throw new DomainException("Student fee is required.");

        StudentFeeId = studentFeeId;
    }

    private static void CheckFee(Money fee)
    {
        if (fee.Amount <= 0)
            throw new DomainException("Bus fee must be greater than zero.");
    }
}
