using SchoolManager.Attendance.Domain;
using AutoMapper;

namespace SchoolManager.Attendance.Application
{
    public class BusMappingProfile : Profile
    {
        public BusMappingProfile()
        {
            CreateMap<Bus, BusDto>();
        }
    }
}
