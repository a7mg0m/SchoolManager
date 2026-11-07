using AutoMapper;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class PaymentMappingProfile : Profile
    {
        public PaymentMappingProfile()
        {
            CreateMap<CreatePaymentDto, CreatePaymentCommand>()
                .ConstructUsing(src => new CreatePaymentCommand(
                    src.StudentFeeId,
                    src.ParentId,
                    src.PaymentMethodId,
                    src.Amount,
                    src.Currency));

            CreateMap<Payment, PaymentDto>()
                .ForMember(d => d.Amount, o => o.MapFrom(s => s.Amount.Amount))
                .ForMember(d => d.Currency, o => o.MapFrom(s => s.Amount.Currency))
                .ForMember(d => d.PaymentMethodName, o => o.MapFrom(s => s.PaymentMethod != null ? s.PaymentMethod.Name : null));
        }
    }
}
