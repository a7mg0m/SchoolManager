using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class GetBusRouteByIdQueryHandler : IQueryHandler<GetBusRouteByIdQuery, BusRouteDto?>
    {
        private readonly IBusRouteRepository _repository;
        private readonly IMapper _mapper;

        public GetBusRouteByIdQueryHandler(IBusRouteRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<BusRouteDto?> Handle(GetBusRouteByIdQuery query, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return entity is null ? null : _mapper.Map<BusRouteDto>(entity);
        }
    }

    public class GetAllBusRoutesQueryHandler : IQueryHandler<GetAllBusRoutesQuery, List<BusRouteDto>>
    {
        private readonly IBusRouteRepository _repository;
        private readonly IMapper _mapper;

        public GetAllBusRoutesQueryHandler(IBusRouteRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<BusRouteDto>> Handle(GetAllBusRoutesQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<List<BusRouteDto>>(entities);
        }
    }

    public class GetActiveBusRoutesQueryHandler : IQueryHandler<GetActiveBusRoutesQuery, List<BusRouteDto>>
    {
        private readonly IBusRouteRepository _repository;
        private readonly IMapper _mapper;

        public GetActiveBusRoutesQueryHandler(IBusRouteRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<BusRouteDto>> Handle(GetActiveBusRoutesQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.SearchAsync(r => r.IsActive);
            return _mapper.Map<List<BusRouteDto>>(entities);
        }
    }
}
