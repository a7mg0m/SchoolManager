using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class GetEmployeesByGroupIdQueryHandler : IQueryHandler<GetEmployeesByGroupIdQuery, List<EmployeeDto>>
    {
        private readonly IEmployeeRepository _repository;
        private readonly IMapper _mapper;

        public GetEmployeesByGroupIdQueryHandler(IEmployeeRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<EmployeeDto>> Handle(GetEmployeesByGroupIdQuery query, CancellationToken cancellationToken)
            => _mapper.Map<List<EmployeeDto>>(await _repository.GetByGroupIdAsync(query.EmployeeGroupId));
    }
}
