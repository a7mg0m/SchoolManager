using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// Admin transport report: the trips between two dates (both included, at most a year), of one bus when given,
    /// with their students' marks; by date then type.
    /// </summary>
    public record GetTripsReportQuery(DateOnly From, DateOnly To, Guid? BusId) : IQuery<List<TripDto>>;

    public class GetTripsReportQueryHandler(ITripRepository trips, IMapper mapper) : IQueryHandler<GetTripsReportQuery, List<TripDto>>
    {
        public const int MaxDays = 366;

        public async Task<List<TripDto>> Handle(GetTripsReportQuery query, CancellationToken cancellationToken)
        {
            var from = query.From;
            var to = query.To < from ? from : query.To;
            if (to.DayNumber - from.DayNumber > MaxDays) to = from.AddDays(MaxDays);

            var busId = query.BusId;
            var list = await trips.SearchAsync(t => t.Date >= from && t.Date <= to && (busId == null || t.BusId == busId));
            return mapper.Map<List<TripDto>>(list.OrderBy(t => t.Date).ThenBy(t => t.Type).ToList());
        }
    }
}
