using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record GetExpenseCategoryByIdQuery(Guid Id) : IQuery<ExpenseCategoryDto?>;

    public record GetAllExpenseCategoriesQuery() : IQuery<List<ExpenseCategoryDto>>;
}
