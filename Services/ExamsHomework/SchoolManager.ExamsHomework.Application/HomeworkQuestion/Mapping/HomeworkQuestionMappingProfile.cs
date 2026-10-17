using AutoMapper;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class HomeworkQuestionMappingProfile : Profile
    {
        public HomeworkQuestionMappingProfile()
        {
            CreateMap<HomeworkQuestion, HomeworkQuestionDto>();
        }
    }
}
