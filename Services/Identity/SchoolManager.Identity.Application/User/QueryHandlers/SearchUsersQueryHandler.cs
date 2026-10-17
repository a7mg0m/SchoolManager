using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class SearchUsersQueryHandler : ICommandHandler<SearchUsersQuery, List<UserDto>>
    {
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;

        public SearchUsersQueryHandler(IUserRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<UserDto>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _repository.SearchAsync(request.SearchTerm);

            return _mapper.Map<List<UserDto>>(users);
        }
    }
}
