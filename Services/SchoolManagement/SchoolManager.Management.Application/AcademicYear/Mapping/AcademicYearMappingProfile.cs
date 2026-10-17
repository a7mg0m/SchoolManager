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
                    Guid.NewGuid()));

			CreateMap<AcademicYear, AcademicYearDto>()
;
		}
	}
}
