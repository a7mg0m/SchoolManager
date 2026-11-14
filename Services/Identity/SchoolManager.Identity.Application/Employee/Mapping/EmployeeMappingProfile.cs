using AutoMapper;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class EmployeeMappingProfile : Profile
    {
        public EmployeeMappingProfile()
        {
            CreateMap<CreateEmployeeDto, CreateEmployeeCommand>()
                .ConstructUsing(src => new CreateEmployeeCommand(
                    src.FullNameAr,
                    src.FullNameEn,
                    src.NationalId,
                    src.PhoneNumber,
                    src.Email,
                    src.Address,
                    src.JobTitle,
                    src.HireDate,
                    src.EmployeeGroupId,
                    src.LoginName,
                    src.Password,
                    src.IsActive));

            CreateMap<UpdateEmployeeDto, UpdateEmployeeCommand>()
                .ConstructUsing(src => new UpdateEmployeeCommand(
                    src.Id,
                    src.FullNameAr,
                    src.FullNameEn,
                    src.NationalId,
                    src.PhoneNumber,
                    src.Email,
                    src.Address,
                    src.JobTitle,
                    src.HireDate,
                    src.EmployeeGroupId,
                    src.LoginName,
                    src.Password));

            CreateMap<Employee, EmployeeDto>()
                .ForMember(d => d.FullNameAr, o => o.MapFrom(s => s.FullName.Arabic))
                .ForMember(d => d.FullNameEn, o => o.MapFrom(s => s.FullName.English))
                .ForMember(d => d.EmployeeGroupNameAr, o => o.MapFrom(s => s.EmployeeGroup != null ? s.EmployeeGroup.Name.Arabic : null))
                .ForMember(d => d.EmployeeGroupNameEn, o => o.MapFrom(s => s.EmployeeGroup != null ? s.EmployeeGroup.Name.English : null))
                .ForMember(d => d.LoginName, o => o.MapFrom(s => s.User != null ? s.User.LoginName : null));
        }
    }
}
