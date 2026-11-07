using AutoMapper;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class ExpenseMappingProfile : Profile
    {
        public ExpenseMappingProfile()
        {
            CreateMap<CreateExpenseDto, CreateExpenseCommand>()
                .ConstructUsing(src => new CreateExpenseCommand(
                    src.Title,
                    src.Description,
                    src.EventId,
                    src.CategoryId,
                    src.Type,
                    src.Amount,
                    src.Currency,
                    src.ExpenseDate,
                    src.Reference,
                    Guid.NewGuid()));

            CreateMap<Expense, ExpenseDto>()
                .ForMember(d => d.Amount, o => o.MapFrom(s => s.Amount.Amount))
                .ForMember(d => d.Currency, o => o.MapFrom(s => s.Amount.Currency))
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null));
        }
    }
}
