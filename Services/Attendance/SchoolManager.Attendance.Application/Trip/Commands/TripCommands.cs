using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// Plans a trip of a bus (one per bus, date and direction; a cancelled one does not count). Every student with
    /// an assignment on the bus running on that date is added.
    /// </summary>
    public record CreateTripCommand(Guid BusId, DateOnly Date, TripType Type) : ICommand<Guid>;

    public record StartTripCommand(Guid Id) : ICommand<bool>;

    public record CompleteTripCommand(Guid Id) : ICommand<bool>;

    public record CancelTripCommand(Guid Id) : ICommand<bool>;

    /// <summary>The bus's current position while the trip runs (parents follow it)</summary>
    public record UpdateTripLocationCommand(Guid Id, double Latitude, double Longitude) : ICommand<bool>;

    /// <summary>A student boarded, was dropped off or did not come (time = now, UTC)</summary>
    public record MarkTripStudentCommand(Guid TripId, Guid StudentId, TripStudentStatus Status) : ICommand<bool>;
}
