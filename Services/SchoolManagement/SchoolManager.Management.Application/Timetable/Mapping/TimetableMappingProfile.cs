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
					src.StageId,
					src.GradeId,
                    src.AcademicYearId,
                    src.SemesterId,
                    src.Slots,
                    src.DayStartTime,
                    src.WorkDays));

            CreateMap<UpdateTimetableDto, UpdateTimetableCommand>()
                .ConstructUsing(src => new UpdateTimetableCommand(
                    src.Id,
                    src.StageId,
                    src.GradeId,
                    src.AcademicYearId,
                    src.SemesterId,
                    src.Slots,
                    src.DayStartTime,
                    src.WorkDays));

            CreateMap<TimetableWorkDay, TimetableWorkDayDto>();

			CreateMap<Timetable, TimetableDto>()
;
		}
	}
}
