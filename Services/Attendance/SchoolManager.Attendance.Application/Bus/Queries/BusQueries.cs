using BuildingBlocks.CQRS;

namespace SchoolManager.Attendance.Application
{
    public record GetBusByIdQuery(Guid Id) : IQuery<BusDto?>;

    public record GetAllBusesQuery() : IQuery<List<BusDto>>;

    public record GetActiveBusesQuery() : IQuery<List<BusDto>>;

    public record GetBusesByRouteQuery(Guid BusRouteId) : IQuery<List<BusDto>>;
}
