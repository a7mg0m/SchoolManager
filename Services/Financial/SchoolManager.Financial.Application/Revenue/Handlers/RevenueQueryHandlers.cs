using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class GetRevenueByIdQueryHandler : IQueryHandler<GetRevenueByIdQuery, RevenueDto?>
    {
        private readonly IRevenueRepository _repository;
        private readonly IMapper _mapper;

        public GetRevenueByIdQueryHandler(IRevenueRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<RevenueDto?> Handle(GetRevenueByIdQuery query, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return entity is null ? null : _mapper.Map<RevenueDto>(entity);
        }
    }

    public class GetAllRevenuesQueryHandler : IQueryHandler<GetAllRevenuesQuery, List<RevenueDto>>
    {
        private readonly IRevenueRepository _repository;
        private readonly IMapper _mapper;

        public GetAllRevenuesQueryHandler(IRevenueRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<RevenueDto>> Handle(GetAllRevenuesQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<List<RevenueDto>>(entities);
        }
    }
}
