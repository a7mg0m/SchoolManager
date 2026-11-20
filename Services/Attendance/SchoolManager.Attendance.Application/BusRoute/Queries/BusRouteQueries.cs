using BuildingBlocks.CQRS;

namespace SchoolManager.Attendance.Application
{
    public record GetBusRouteByIdQuery(Guid Id) : IQuery<BusRouteDto?>;

    public record GetAllBusRoutesQuery() : IQuery<List<BusRouteDto>>;

    public record GetActiveBusRoutesQuery() : IQuery<List<BusRouteDto>>;
}
