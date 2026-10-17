using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class SemesterMappingProfile : Profile
	{
		public SemesterMappingProfile()
		{
			CreateMap<CreateSemesterDto, CreateSemesterCommand>()
				.ConstructUsing(src => new CreateSemesterCommand(
					src.Name,
                    src.AcademicYearId,
                    Guid.NewGuid()));

            CreateMap<UpdateSemesterDto, UpdateSemesterCommand>()
                .ConstructUsing(src => new UpdateSemesterCommand(
                    src.Id,
                    src.Name,
                    src.AcademicYearId,
                    Guid.NewGuid()));

			CreateMap<Semester, SemesterDto>()
;
		}
	}
}
