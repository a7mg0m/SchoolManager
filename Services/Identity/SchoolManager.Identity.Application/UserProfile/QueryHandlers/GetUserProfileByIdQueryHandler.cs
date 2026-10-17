using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class GetUserProfileByIdQueryHandler : ICommandHandler<GetUserProfileByIdQuery, UserProfileDto?>
    {
        private readonly IUserProfileRepository _repository;
        private readonly IMapper _mapper;

        public GetUserProfileByIdQueryHandler(IUserProfileRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<UserProfileDto?> Handle(GetUserProfileByIdQuery request, CancellationToken cancellationToken)
        {
            var profile = await _repository.GetByIdAsync(request.Id);

            return profile is null ? null : _mapper.Map<UserProfileDto>(profile);
        }
    }
}
