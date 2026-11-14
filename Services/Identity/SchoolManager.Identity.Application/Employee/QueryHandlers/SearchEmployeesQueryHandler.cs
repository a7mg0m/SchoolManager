using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class SearchEmployeesQueryHandler : IQueryHandler<SearchEmployeesQuery, List<EmployeeDto>>
    {
        private readonly IEmployeeRepository _repository;
        private readonly IMapper _mapper;

        public SearchEmployeesQueryHandler(IEmployeeRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<EmployeeDto>> Handle(SearchEmployeesQuery query, CancellationToken cancellationToken)
            => _mapper.Map<List<EmployeeDto>>(await _repository.SearchAsync(query.SearchTerm));
    }
}
