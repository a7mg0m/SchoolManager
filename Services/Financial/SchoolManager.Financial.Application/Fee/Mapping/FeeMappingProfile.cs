using AutoMapper;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class FeeMappingProfile : Profile
    {
        public FeeMappingProfile()
        {
            CreateMap<CreateFeeDto, CreateFeeCommand>()
                .ConstructUsing(src => new CreateFeeCommand(
                    src.GradeId,
                    src.Name,
                    src.Type,
                    src.Amount,
                    src.Currency,
                    src.IsMandatory));

            CreateMap<Fee, FeeDto>()
                .ForMember(d => d.Amount, o => o.MapFrom(s => s.Amount.Amount))
                .ForMember(d => d.Currency, o => o.MapFrom(s => s.Amount.Currency));
        }
    }
}
