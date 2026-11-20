using BuildingBlocks;

namespace SchoolManager.Attendance.Domain;

/// <summary>A school bus, its driver and the route it drives</summary>
public sealed class Bus : BaseEntity
{
    public string Number { get; private set; } = default!;

    /// <summary>رقم اللوحة</summary>
    public string PlateNumber { get; private set; } = default!;

    /// <summary>Seats for students (greater than zero)</summary>
    public int Capacity { get; private set; }
    public string DriverName { get; private set; } = default!;
    public string DriverPhone { get; private set; } = default!;
    public Guid BusRouteId { get; private set; }
    public bool IsActive { get; private set; }
    public AuditInfo AuditInfo { get; private set; } = default!;

    private Bus() { }

    public static Bus Create(
        string number, string plateNumber, int capacity, string driverName, string driverPhone, Guid busRouteId, AuditInfo auditInfo)
    {
        var bus = new Bus { IsActive = true };
        bus.Update(number, plateNumber, capacity, driverName, driverPhone, busRouteId, auditInfo);
        return bus;
    }

    public void Update(
        string number, string plateNumber, int capacity, string driverName, string driverPhone, Guid busRouteId, AuditInfo auditInfo)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new DomainException("Bus number is required.");
        if (string.IsNullOrWhiteSpace(plateNumber))
            throw new DomainException("Plate number is required.");
        if (capacity <= 0)
            throw new DomainException("Capacity must be greater than zero.");
        if (string.IsNullOrWhiteSpace(driverName))
            throw new DomainException("Driver name is required.");
        if (string.IsNullOrWhiteSpace(driverPhone))
            throw new DomainException("Driver phone is required.");
        if (busRouteId == Guid.Empty)
            throw new DomainException("Bus route is required.");

        Number = number.Trim();
        PlateNumber = plateNumber.Trim();
        Capacity = capacity;
        DriverName = driverName.Trim();
        DriverPhone = driverPhone.Trim();
        BusRouteId = busRouteId;
        AuditInfo = auditInfo;
    }

    public void Activate(AuditInfo auditInfo)
    {
        IsActive = true;
        AuditInfo = auditInfo;
    }

    public void Deactivate(AuditInfo auditInfo)
    {
        IsActive = false;
        AuditInfo = auditInfo;
    }
}
