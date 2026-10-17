using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class SearchUserProfilesQueryHandler : ICommandHandler<SearchUserProfilesQuery, List<UserProfileDto>>
    {
        private readonly IUserProfileRepository _repository;
        private readonly IMapper _mapper;

        public SearchUserProfilesQueryHandler(IUserProfileRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<UserProfileDto>> Handle(SearchUserProfilesQuery request, CancellationToken cancellationToken)
        {
            var profiles = await _repository.SearchAsync(request.SearchTerm);

            return _mapper.Map<List<UserProfileDto>>(profiles);
        }
    }
}
