using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class TimetableMappingProfile : Profile
	{
		public TimetableMappingProfile()
		{
			CreateMap<CreateTimetableDto, CreateTimetableCommand>()
				.ConstructUsing(src => new CreateTimetableCommand(
					src.GradeId,
                    src.AcademicYearId,
                    Guid.NewGuid()));

            CreateMap<UpdateTimetableDto, UpdateTimetableCommand>()
                .ConstructUsing(src => new UpdateTimetableCommand(
                    src.Id,
                    src.GradeId,
                    src.AcademicYearId,
                    Guid.NewGuid()));

			CreateMap<Timetable, TimetableDto>()
;
		}
	}
}
