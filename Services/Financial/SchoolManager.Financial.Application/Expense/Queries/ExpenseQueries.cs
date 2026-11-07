using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record GetExpenseByIdQuery(Guid Id) : IQuery<ExpenseDto?>;

    public record GetAllExpensesQuery() : IQuery<List<ExpenseDto>>;
}
