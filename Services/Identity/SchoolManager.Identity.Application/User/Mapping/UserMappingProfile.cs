using SchoolManager.Identity.Domain;
using AutoMapper;


namespace SchoolManager.Identity.Application
{
    public class UserMappingProfile:Profile
    {
        public UserMappingProfile()
        {
            // DTO → Command (Create)
            CreateMap<CreateUserDto, CreateUserCommand>()
                .ConstructUsing(src => new CreateUserCommand(
                    src.LoginName,
                    src.Email,
                    src.HashPassword,
                    src.RoleId,
                    Guid.NewGuid()));

            // DTO → Command (Login)
            CreateMap<LoginDto, LoginCommand>();

            // DTO → Command (Update)
            CreateMap<UpdateUserDto, UpdateUserCommand>()
                .ConstructUsing(src => new UpdateUserCommand(
                    src.Id,
                    src.LoginName,
                    null,
                    src.RoleId,
                    Guid.NewGuid()));

            // Entity
            CreateMap<User, UserDto>();
        }
    }
}
