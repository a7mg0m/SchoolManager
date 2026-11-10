using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class AcademicYearMappingProfile : Profile
	{
		public AcademicYearMappingProfile()
		{
			CreateMap<CreateAcademicYearDto, CreateAcademicYearCommand>()
				.ConstructUsing(src => new CreateAcademicYearCommand(
					src.Name,
                    src.StartDate,
                    src.EndDate,
                    src.Semesters,
                    Guid.NewGuid()));

			CreateMap<UpdateAcademicYearDto, UpdateAcademicYearCommand>()
				.ConstructUsing(src => new UpdateAcademicYearCommand(
					src.Id,
					src.Name,
					src.StartDate,
					src.EndDate,
					src.IsActive,
					src.Semesters,
					Guid.NewGuid()));

			CreateMap<AcademicYear, AcademicYearDto>()
;
		}
	}
}
