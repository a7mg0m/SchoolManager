using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class GetBusByIdQueryHandler : IQueryHandler<GetBusByIdQuery, BusDto?>
    {
        private readonly IBusRepository _repository;
        private readonly IMapper _mapper;

        public GetBusByIdQueryHandler(IBusRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<BusDto?> Handle(GetBusByIdQuery query, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return entity is null ? null : _mapper.Map<BusDto>(entity);
        }
    }

    public class GetAllBusesQueryHandler : IQueryHandler<GetAllBusesQuery, List<BusDto>>
    {
        private readonly IBusRepository _repository;
        private readonly IMapper _mapper;

        public GetAllBusesQueryHandler(IBusRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<BusDto>> Handle(GetAllBusesQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<List<BusDto>>(entities.OrderBy(b => b.Number).ToList());
        }
    }

    public class GetActiveBusesQueryHandler : IQueryHandler<GetActiveBusesQuery, List<BusDto>>
    {
        private readonly IBusRepository _repository;
        private readonly IMapper _mapper;

        public GetActiveBusesQueryHandler(IBusRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<BusDto>> Handle(GetActiveBusesQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.SearchAsync(b => b.IsActive);
            return _mapper.Map<List<BusDto>>(entities.OrderBy(b => b.Number).ToList());
        }
    }

    public class GetBusesByRouteQueryHandler : IQueryHandler<GetBusesByRouteQuery, List<BusDto>>
    {
        private readonly IBusRepository _repository;
        private readonly IMapper _mapper;

        public GetBusesByRouteQueryHandler(IBusRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<BusDto>> Handle(GetBusesByRouteQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.SearchAsync(b => b.BusRouteId == query.BusRouteId);
            return _mapper.Map<List<BusDto>>(entities.OrderBy(b => b.Number).ToList());
        }
    }
}
