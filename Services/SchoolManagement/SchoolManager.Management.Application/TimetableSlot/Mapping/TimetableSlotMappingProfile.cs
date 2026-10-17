using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class TimetableSlotMappingProfile : Profile
	{
		public TimetableSlotMappingProfile()
		{
			CreateMap<CreateTimetableSlotDto, CreateTimetableSlotCommand>()
				.ConstructUsing(src => new CreateTimetableSlotCommand(
					src.Day,
                    src.NameAr,
                    src.NameEn,
                    src.StartTime,
                    src.EndTime,
                    src.SubjectId,
                    src.TeacherId,
                    src.GradeId,
                    src.SectionId,
                    Guid.NewGuid()));

            CreateMap<UpdateTimetableSlotDto, UpdateTimetableSlotCommand>()
                .ConstructUsing(src => new UpdateTimetableSlotCommand(
                    src.Id,
                    src.Day,
                    src.NameAr,
                    src.NameEn,
                    src.StartTime,
                    src.EndTime,
                    src.GradeId,
                    src.SectionId,
                    src.SubjectId,
                    src.TeacherId,
                    Guid.NewGuid()));

			CreateMap<TimetableSlot, TimetableSlotDto>()
                .ForMember(d => d.NameAr, o => o.MapFrom(s => s.Name.Arabic))
                .ForMember(d => d.NameEn, o => o.MapFrom(s => s.Name.English));
		}
	}
}
