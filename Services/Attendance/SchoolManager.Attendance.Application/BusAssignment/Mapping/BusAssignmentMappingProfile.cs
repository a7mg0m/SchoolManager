using SchoolManager.Attendance.Domain;
using AutoMapper;

namespace SchoolManager.Attendance.Application
{
    public class BusAssignmentMappingProfile : Profile
    {
        public BusAssignmentMappingProfile()
        {
            CreateMap<BusAssignment, BusAssignmentDto>()
                .ForMember(d => d.FeeAmount, o => o.MapFrom(s => s.Fee.Amount))
                .ForMember(d => d.Currency, o => o.MapFrom(s => s.Fee.Currency));
        }
    }
}
