using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class GetAllEmployeesQueryHandler : IQueryHandler<GetAllEmployeesQuery, List<EmployeeDto>>
    {
        private readonly IEmployeeRepository _repository;
        private readonly IMapper _mapper;

        public GetAllEmployeesQueryHandler(IEmployeeRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<EmployeeDto>> Handle(GetAllEmployeesQuery query, CancellationToken cancellationToken)
            => _mapper.Map<List<EmployeeDto>>(await _repository.GetAllWithGroupAsync());
    }
}
