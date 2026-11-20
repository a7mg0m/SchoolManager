using BuildingBlocks.CQRS;

namespace SchoolManager.Attendance.Application
{
    /// <summary>New route with its stops (each stop's Order unique)</summary>
    public record CreateBusRouteCommand(
        string NameAr,
        string NameEn,
        string? Description,
        List<BusRouteStopDto> Stops
    ) : ICommand<Guid>;

    /// <summary>Updates the route; the stops sent replace the saved ones</summary>
    public record UpdateBusRouteCommand(
        Guid Id,
        string NameAr,
        string NameEn,
        string? Description,
        List<BusRouteStopDto> Stops
    ) : ICommand<bool>;

    /// <summary>Not allowed while buses use the route</summary>
    public record DeleteBusRouteCommand(Guid Id) : ICommand<bool>;

    public record SetBusRouteActiveStatusCommand(Guid Id, bool IsActive) : ICommand<bool>;
}
