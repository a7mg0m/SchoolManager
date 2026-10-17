using AutoMapper;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class HomeworkPublishMappingProfile : Profile
    {
        public HomeworkPublishMappingProfile()
        {
            CreateMap<HomeworkPublish, HomeworkPublishDto>();
        }
    }
}
