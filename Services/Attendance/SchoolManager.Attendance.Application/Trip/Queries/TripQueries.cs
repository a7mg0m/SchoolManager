using BuildingBlocks.CQRS;

namespace SchoolManager.Attendance.Application
{
    public record GetTripByIdQuery(Guid Id) : IQuery<TripDto?>;

    /// <param name="BusId">One bus only; null = every bus</param>
    public record GetTripsByDateQuery(DateOnly Date, Guid? BusId = null) : IQuery<List<TripDto>>;

    /// <summary>
    /// Parent app: today's running trip of one of the logged-in parent's children, to follow the bus;
    /// null when the child is on no running trip.
    /// </summary>
    public record GetMyChildCurrentTripQuery(Guid StudentId, string Authorization) : IQuery<TripTrackingDto?>;
}
