using SchoolManager.Attendance.Domain;
using AutoMapper;

namespace SchoolManager.Attendance.Application
{
    public class StudentEvaluationMappingProfile : Profile
    {
        public StudentEvaluationMappingProfile()
        {
            CreateMap<StudentEvaluation, StudentEvaluationDto>();
        }
    }
}
