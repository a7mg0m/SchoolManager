using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class TeacherMappingProfile : Profile
	{
		public TeacherMappingProfile()
		{
			CreateMap<CreateTeacherDto, CreateTeacherCommand>()
				.ConstructUsing(src => new CreateTeacherCommand(
					src.FullNameAr,
                    src.FullNameEn,
                    src.DateOfBirth,
                    src.NationalId,
                    src.Gender,
                    src.PhoneNumber,
                    src.Email,
                    src.Address,
                    Guid.NewGuid()));

            CreateMap<UpdateTeacherDto, UpdateTeacherCommand>()
                .ConstructUsing(src => new UpdateTeacherCommand(
                    src.Id,
                    src.FullNameAr,
                    src.FullNameEn,
                    src.PhoneNumber,
                    src.Email,
                    src.Address,
                    Guid.NewGuid()));

			CreateMap<Teacher, TeacherDto>()
                .ForMember(d => d.FullNameAr, o => o.MapFrom(s => s.FullName.Arabic))
                .ForMember(d => d.FullNameEn, o => o.MapFrom(s => s.FullName.English));
		}
	}
}
