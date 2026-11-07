using SchoolManager.Financial.Domain;
using AutoMapper;

namespace SchoolManager.Financial.Application
{
    public class StudentFeeMappingProfile : Profile
    {
        public StudentFeeMappingProfile()
        {
            CreateMap<StudentFee, StudentFeeDto>()
                .ForMember(d => d.TotalAmount, o => o.MapFrom(s => s.TotalAmount.Amount))
                .ForMember(d => d.DiscountAmount, o => o.MapFrom(s => s.DiscountAmount.Amount))
                .ForMember(d => d.PaidAmount, o => o.MapFrom(s => s.PaidAmount.Amount))
                .ForMember(d => d.NetAmount, o => o.MapFrom(s => s.NetAmount.Amount))
                .ForMember(d => d.Balance, o => o.MapFrom(s => s.Balance.Amount))
                .ForMember(d => d.Currency, o => o.MapFrom(s => s.TotalAmount.Currency));
        }
    }
}
