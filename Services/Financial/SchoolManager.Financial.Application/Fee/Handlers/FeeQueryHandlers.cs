using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class GetFeeByIdQueryHandler : IQueryHandler<GetFeeByIdQuery, FeeDto?>
    {
        private readonly IFeesRepository _repository;
        private readonly IMapper _mapper;

        public GetFeeByIdQueryHandler(IFeesRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<FeeDto?> Handle(GetFeeByIdQuery query, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return entity is null ? null : _mapper.Map<FeeDto>(entity);
        }
    }

    public class GetAllFeesQueryHandler : IQueryHandler<GetAllFeesQuery, List<FeeDto>>
    {
        private readonly IFeesRepository _repository;
        private readonly IMapper _mapper;

        public GetAllFeesQueryHandler(IFeesRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<FeeDto>> Handle(GetAllFeesQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<List<FeeDto>>(entities);
        }
    }
}
