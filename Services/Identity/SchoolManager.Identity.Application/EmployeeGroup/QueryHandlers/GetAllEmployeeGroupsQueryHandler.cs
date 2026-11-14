using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class GetAllEmployeeGroupsQueryHandler : IQueryHandler<GetAllEmployeeGroupsQuery, List<EmployeeGroupDto>>
    {
        private readonly IEmployeeGroupRepository _repository;
        private readonly IMapper _mapper;

        public GetAllEmployeeGroupsQueryHandler(IEmployeeGroupRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<EmployeeGroupDto>> Handle(GetAllEmployeeGroupsQuery query, CancellationToken cancellationToken)
        {
            var groups = await _repository.GetAllAsync();
            return await EmployeeGroupMapper.MapWithCountsAsync(groups, _repository, _mapper);
        }
    }
}
