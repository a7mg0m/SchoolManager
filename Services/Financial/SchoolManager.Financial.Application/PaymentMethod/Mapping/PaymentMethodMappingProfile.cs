using AutoMapper;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class PaymentMethodMappingProfile : Profile
    {
        public PaymentMethodMappingProfile()
        {
            CreateMap<CreatePaymentMethodDto, CreatePaymentMethodCommand>()
                .ConstructUsing(src => new CreatePaymentMethodCommand(
                    src.Name,
                    src.Type));

            CreateMap<PaymentMethod, PaymentMethodDto>();
        }
    }
}
