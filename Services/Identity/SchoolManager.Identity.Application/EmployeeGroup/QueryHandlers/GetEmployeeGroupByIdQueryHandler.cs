using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class GetEmployeeGroupByIdQueryHandler : IQueryHandler<GetEmployeeGroupByIdQuery, EmployeeGroupDto?>
    {
        private readonly IEmployeeGroupRepository _repository;
        private readonly IMapper _mapper;

        public GetEmployeeGroupByIdQueryHandler(IEmployeeGroupRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<EmployeeGroupDto?> Handle(GetEmployeeGroupByIdQuery query, CancellationToken cancellationToken)
        {
            var group = await _repository.GetByIdAsync(query.Id);
            if (group is null)
                return null;

            return (await EmployeeGroupMapper.MapWithCountsAsync([group], _repository, _mapper)).Single();
        }
    }
}
