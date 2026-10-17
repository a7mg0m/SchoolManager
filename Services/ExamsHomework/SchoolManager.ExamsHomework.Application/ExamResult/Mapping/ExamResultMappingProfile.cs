using AutoMapper;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class ExamResultMappingProfile : Profile
    {
        public ExamResultMappingProfile()
        {
            CreateMap<ExamResult, ExamResultDto>();
        }
    }
}
