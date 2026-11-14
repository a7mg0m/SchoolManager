using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class GetEmployeeByIdQueryHandler : IQueryHandler<GetEmployeeByIdQuery, EmployeeDto?>
    {
        private readonly IEmployeeRepository _repository;
        private readonly IMapper _mapper;

        public GetEmployeeByIdQueryHandler(IEmployeeRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<EmployeeDto?> Handle(GetEmployeeByIdQuery query, CancellationToken cancellationToken)
        {
            var employee = await _repository.GetByIdWithGroupAsync(query.Id);
            return employee is null ? null : _mapper.Map<EmployeeDto>(employee);
        }
    }
}
