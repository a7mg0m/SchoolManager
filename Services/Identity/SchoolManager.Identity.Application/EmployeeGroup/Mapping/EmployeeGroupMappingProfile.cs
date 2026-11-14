using AutoMapper;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class EmployeeGroupMappingProfile : Profile
    {
        public EmployeeGroupMappingProfile()
        {
            CreateMap<CreateEmployeeGroupDto, CreateEmployeeGroupCommand>()
                .ConstructUsing(src => new CreateEmployeeGroupCommand(
                    src.NameAr,
                    src.NameEn,
                    src.Description,
                    src.IsActive));

            CreateMap<UpdateEmployeeGroupDto, UpdateEmployeeGroupCommand>()
                .ConstructUsing(src => new UpdateEmployeeGroupCommand(
                    src.Id,
                    src.NameAr,
                    src.NameEn,
                    src.Description));

            CreateMap<EmployeeGroup, EmployeeGroupDto>()
                .ForMember(d => d.NameAr, o => o.MapFrom(s => s.Name.Arabic))
                .ForMember(d => d.NameEn, o => o.MapFrom(s => s.Name.English))
                .ForMember(d => d.EmployeesCount, o => o.Ignore());
        }
    }
}
