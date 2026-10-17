using AutoMapper;

namespace SchoolManager.Management.Application
{
    public class SchoolInfoMappingProfile : Profile
    {
        public SchoolInfoMappingProfile()
        {
            CreateMap<CreateSchoolInfoDto, CreateSchoolInfoCommand>()
                .ConstructUsing(src => new CreateSchoolInfoCommand(
                    src.NameAr,
                    src.NameEn,
                    src.Address,
                    src.Phone,
                    src.Email,
                    src.Logo,
                    Guid.NewGuid()));

            CreateMap<UpdateSchoolInfoDto, UpdateSchoolInfoCommand>()
                .ConstructUsing(src => new UpdateSchoolInfoCommand(
                    src.Id,
                    src.NameAr,
                    src.NameEn,
                    src.Address,
                    src.Phone,
                    src.Email,
                    src.Logo,
                    Guid.NewGuid()));

            CreateMap<Domain.SchoolInfo, SchoolInfoDto>()
                .ForMember(d => d.NameAr, o => o.MapFrom(s => s.Name.Arabic))
                .ForMember(d => d.NameEn, o => o.MapFrom(s => s.Name.English));
        }
    }
}
