using AutoMapper;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class HomeworkMappingProfile : Profile
    {
        public HomeworkMappingProfile()
        {
            CreateMap<Homework, HomeworkDto>();
        }
    }
}
