using AutoMapper;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class UserProfileMappingProfile : Profile
    {
        public UserProfileMappingProfile()
        {
            CreateMap<CreateUserProfileDto, CreateUserProfileCommand>()
                .ConstructUsing(src => new CreateUserProfileCommand(
                    src.UserId,
                    src.FirstName,
                    src.LastName,
                    src.Email,
                    src.PhoneNumber,
                    src.Address,
                    src.ProfileImage,
                    Guid.NewGuid()));

            CreateMap<UpdateUserProfileDto, UpdateUserProfileCommand>()
                .ConstructUsing(src => new UpdateUserProfileCommand(
                    src.Id,
                    src.FirstName,
                    src.LastName,
                    src.Email,
                    src.PhoneNumber,
                    src.Address,
                    src.ProfileImage,
                    Guid.NewGuid()));

            CreateMap<UserProfile, UserProfileDto>();
        }
    }
}
