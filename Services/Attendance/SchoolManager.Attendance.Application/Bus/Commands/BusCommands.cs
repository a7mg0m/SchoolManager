using BuildingBlocks.CQRS;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// New bus; the plate number must be unique and the route must exist.
    /// The driver gets a login (Identity service) with DriverLoginName / DriverPassword (both required).
    /// </summary>
    public record CreateBusCommand(
        string Number,
        string PlateNumber,
        int Capacity,
        string DriverName,
        string DriverPhone,
        Guid BusRouteId,
        string? DriverLoginName = null,
        string? DriverPassword = null
    ) : ICommand<Guid>;

    /// <summary>
    /// The capacity cannot drop below the bus's active assignments.
    /// DriverPassword empty keeps the driver's current password (required when the bus has no driver login yet).
    /// </summary>
    public record UpdateBusCommand(
        Guid Id,
        string Number,
        string PlateNumber,
        int Capacity,
        string DriverName,
        string DriverPhone,
        Guid BusRouteId,
        string? DriverLoginName = null,
        string? DriverPassword = null
    ) : ICommand<bool>;

    /// <summary>Not allowed while the bus has active assignments</summary>
    public record DeleteBusCommand(Guid Id) : ICommand<bool>;

    public record SetBusActiveStatusCommand(Guid Id, bool IsActive) : ICommand<bool>;
}
