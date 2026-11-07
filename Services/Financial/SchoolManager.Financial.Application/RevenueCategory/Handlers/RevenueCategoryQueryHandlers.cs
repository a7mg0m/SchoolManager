using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class GetRevenueCategoryByIdQueryHandler : IQueryHandler<GetRevenueCategoryByIdQuery, RevenueCategoryDto?>
    {
        private readonly IRevenueCategoryRepository _repository;
        private readonly IMapper _mapper;

        public GetRevenueCategoryByIdQueryHandler(IRevenueCategoryRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<RevenueCategoryDto?> Handle(GetRevenueCategoryByIdQuery query, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return entity is null ? null : _mapper.Map<RevenueCategoryDto>(entity);
        }
    }

    public class GetAllRevenueCategoriesQueryHandler : IQueryHandler<GetAllRevenueCategoriesQuery, List<RevenueCategoryDto>>
    {
        private readonly IRevenueCategoryRepository _repository;
        private readonly IMapper _mapper;

        public GetAllRevenueCategoriesQueryHandler(IRevenueCategoryRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<RevenueCategoryDto>> Handle(GetAllRevenueCategoriesQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<List<RevenueCategoryDto>>(entities);
        }
    }
}
