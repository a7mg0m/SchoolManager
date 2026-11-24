using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>Driver app: the trips of one bus between two dates (both included), newest first</summary>
    public record GetBusTripsQuery(Guid BusId, DateOnly From, DateOnly To) : IQuery<List<TripDto>>;

    public class GetBusTripsQueryHandler : IQueryHandler<GetBusTripsQuery, List<TripDto>>
    {
        private readonly ITripRepository _repository;
        private readonly IMapper _mapper;

        public GetBusTripsQueryHandler(ITripRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<TripDto>> Handle(GetBusTripsQuery query, CancellationToken cancellationToken)
        {
            var busId = query.BusId;
            var from = query.From;
            var to = query.To;
            var trips = await _repository.SearchAsync(t => t.BusId == busId && t.Date >= from && t.Date <= to);
            return _mapper.Map<List<TripDto>>(trips
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Type)
                .ToList());
        }
    }
}
