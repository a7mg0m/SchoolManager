using AutoMapper;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class ExpenseCategoryMappingProfile : Profile
    {
        public ExpenseCategoryMappingProfile()
        {
            CreateMap<CreateExpenseCategoryDto, CreateExpenseCategoryCommand>()
                .ConstructUsing(src => new CreateExpenseCategoryCommand(
                    src.Name,
                    src.Description,
                    Guid.NewGuid()));

            CreateMap<ExpenseCategory, ExpenseCategoryDto>();
        }
    }
}
