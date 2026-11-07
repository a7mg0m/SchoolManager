using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class GetExpenseCategoryByIdQueryHandler : IQueryHandler<GetExpenseCategoryByIdQuery, ExpenseCategoryDto?>
    {
        private readonly IExpenseCategoryRepository _repository;
        private readonly IMapper _mapper;

        public GetExpenseCategoryByIdQueryHandler(IExpenseCategoryRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ExpenseCategoryDto?> Handle(GetExpenseCategoryByIdQuery query, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return entity is null ? null : _mapper.Map<ExpenseCategoryDto>(entity);
        }
    }

    public class GetAllExpenseCategoriesQueryHandler : IQueryHandler<GetAllExpenseCategoriesQuery, List<ExpenseCategoryDto>>
    {
        private readonly IExpenseCategoryRepository _repository;
        private readonly IMapper _mapper;

        public GetAllExpenseCategoriesQueryHandler(IExpenseCategoryRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<ExpenseCategoryDto>> Handle(GetAllExpenseCategoriesQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<List<ExpenseCategoryDto>>(entities);
        }
    }
}
