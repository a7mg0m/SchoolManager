using SchoolManager.Financial.Domain;
using AutoMapper;

namespace SchoolManager.Financial.Application
{
    public class RevenueCategoryMappingProfile : Profile
    {
        public RevenueCategoryMappingProfile()
        {
            CreateMap<CreateRevenueCategoryDto, CreateRevenueCategoryCommand>()
                .ConstructUsing(src => new CreateRevenueCategoryCommand(
                    src.Name,
                    src.Description));

            CreateMap<RevenueCategory, RevenueCategoryDto>();
        }
    }
}
