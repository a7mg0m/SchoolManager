using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class GetExpenseByIdQueryHandler : IQueryHandler<GetExpenseByIdQuery, ExpenseDto?>
    {
        private readonly IExpenseRepository _repository;
        private readonly IMapper _mapper;

        public GetExpenseByIdQueryHandler(IExpenseRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ExpenseDto?> Handle(GetExpenseByIdQuery query, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return entity is null ? null : _mapper.Map<ExpenseDto>(entity);
        }
    }

    public class GetAllExpensesQueryHandler : IQueryHandler<GetAllExpensesQuery, List<ExpenseDto>>
    {
        private readonly IExpenseRepository _repository;
        private readonly IMapper _mapper;

        public GetAllExpensesQueryHandler(IExpenseRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<ExpenseDto>> Handle(GetAllExpensesQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<List<ExpenseDto>>(entities);
        }
    }
}
