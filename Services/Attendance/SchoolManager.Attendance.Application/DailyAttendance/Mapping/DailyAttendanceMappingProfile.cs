using SchoolManager.Attendance.Domain;
using AutoMapper;

namespace SchoolManager.Attendance.Application
{
    public class DailyAttendanceMappingProfile : Profile
    {
        public DailyAttendanceMappingProfile()
        {
            // StudentId comes from the period's day (set below, or by the handler)
            CreateMap<PeriodAttendance, PeriodAttendanceDto>()
                .ForMember(d => d.StudentId, o => o.Ignore());

            CreateMap<DailyAttendance, DailyAttendanceDto>()
                .ForMember(d => d.AttendedPeriodsCount, o => o.MapFrom(s => s.AttendedPeriodsCount))
                .ForMember(d => d.AbsentPeriodsCount, o => o.MapFrom(s => s.AbsentPeriodsCount))
                .ForMember(d => d.Periods, o => o.MapFrom(s => s.Periods.OrderBy(p => p.StartTime)))
                .AfterMap((s, d) => d.Periods.ForEach(p => p.StudentId = s.StudentId));
        }
    }
}
