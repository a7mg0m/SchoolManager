using SchoolManager.Attendance.Domain;
using AutoMapper;

namespace SchoolManager.Attendance.Application
{
    public class BusRouteMappingProfile : Profile
    {
        public BusRouteMappingProfile()
        {
            CreateMap<BusRouteStop, BusRouteStopDto>()
                .ForMember(d => d.NameAr, o => o.MapFrom(s => s.Name.Arabic))
                .ForMember(d => d.NameEn, o => o.MapFrom(s => s.Name.English));

            CreateMap<BusRoute, BusRouteDto>()
                .ForMember(d => d.NameAr, o => o.MapFrom(s => s.Name.Arabic))
                .ForMember(d => d.NameEn, o => o.MapFrom(s => s.Name.English))
                .ForMember(d => d.Stops, o => o.MapFrom(s => s.Stops.OrderBy(x => x.Order)));
        }
    }
}
