using SchoolManager.Attendance.Domain;
using AutoMapper;

namespace SchoolManager.Attendance.Application
{
    public class EvaluationRequestMappingProfile : Profile
    {
        public EvaluationRequestMappingProfile()
        {
            CreateMap<EvaluationRequest, EvaluationRequestDto>();
        }
    }
}
