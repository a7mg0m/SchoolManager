using AutoMapper;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class ExamResultMappingProfile : Profile
    {
        public ExamResultMappingProfile()
        {
            CreateMap<ExamResult, ExamResultDto>()
                .ForMember(d => d.IsPassed, o => o.Ignore()); // set from the exam's SuccessScore in the query handlers
        }
    }
}
