using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class GetAllUserProfilesQueryHandler : ICommandHandler<GetAllUserProfilesQuery, List<UserProfileDto>>
    {
        private readonly IUserProfileRepository _repository;

        public GetAllUserProfilesQueryHandler(IUserProfileRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<UserProfileDto>> Handle(GetAllUserProfilesQuery request, CancellationToken cancellationToken)
        {
            var profiles = await _repository.GetAllAsync();

            return profiles.Select(p => new UserProfileDto
            {
                Id = p.Id,
                UserId = p.UserId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                PhoneNumber = p.PhoneNumber,
                Address = p.Address,
                ProfileImage = p.ProfileImage
            }).ToList();
        }
    }
}
