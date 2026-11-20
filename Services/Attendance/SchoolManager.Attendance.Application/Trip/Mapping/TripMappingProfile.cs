using SchoolManager.Attendance.Domain;
using AutoMapper;

namespace SchoolManager.Attendance.Application
{
    public class TripMappingProfile : Profile
    {
        public TripMappingProfile()
        {
            CreateMap<TripStudent, TripStudentDto>();
            CreateMap<Trip, TripDto>();
        }
    }
}
