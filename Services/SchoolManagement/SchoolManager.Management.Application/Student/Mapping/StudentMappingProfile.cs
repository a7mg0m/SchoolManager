using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class StudentMappingProfile : Profile
	{
		public StudentMappingProfile()
		{
			CreateMap<CreateStudentDto, CreateStudentCommand>()
				.ConstructUsing(src => new CreateStudentCommand(
					src.FullNameAr,
                    src.FullNameEn,
                    src.DateOfBirth,
                    src.NationalId,
                    src.Gender,
                    src.PhoneNumber,
                    src.Email,
                    src.Address,
                    src.GradeId,
                    src.SectionId,
                    src.AcademicYearId,
                    src.ParentId,
                    Guid.NewGuid()));

            CreateMap<UpdateStudentDto, UpdateStudentCommand>()
                .ConstructUsing(src => new UpdateStudentCommand(
                    src.Id,
                    src.FullNameAr,
                    src.FullNameEn,
                    src.PhoneNumber,
                    src.Email,
                    src.Address,
                    src.GradeId,
                    src.SectionId,
                    src.AcademicYearId,
                    Guid.NewGuid()));

			CreateMap<Student, StudentDto>()
                .ForMember(d => d.FullNameAr, o => o.MapFrom(s => s.FullName.Arabic))
                .ForMember(d => d.FullNameEn, o => o.MapFrom(s => s.FullName.English));
		}
	}
}
