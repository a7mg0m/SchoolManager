using AutoMapper;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class RevenueMappingProfile : Profile
    {
        public RevenueMappingProfile()
        {
            CreateMap<CreateRevenueDto, CreateRevenueCommand>()
                .ConstructUsing(src => new CreateRevenueCommand(
                    src.Title,
                    src.Description,
                    src.CategoryId,
                    src.Type,
                    src.Amount,
                    src.Currency,
                    src.RevenueDate,
                    src.Reference,
                    Guid.NewGuid()));

            CreateMap<Revenue, RevenueDto>()
                .ForMember(d => d.Amount, o => o.MapFrom(s => s.Amount.Amount))
                .ForMember(d => d.Currency, o => o.MapFrom(s => s.Amount.Currency))
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null));
        }
    }
}
