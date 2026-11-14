using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class GetActiveEmployeeGroupsQueryHandler : IQueryHandler<GetActiveEmployeeGroupsQuery, List<EmployeeGroupDto>>
    {
        private readonly IEmployeeGroupRepository _repository;
        private readonly IMapper _mapper;

        public GetActiveEmployeeGroupsQueryHandler(IEmployeeGroupRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<EmployeeGroupDto>> Handle(GetActiveEmployeeGroupsQuery query, CancellationToken cancellationToken)
        {
            var groups = await _repository.SearchAsync(g => g.IsActive);
            return await EmployeeGroupMapper.MapWithCountsAsync(groups, _repository, _mapper);
        }
    }
}
