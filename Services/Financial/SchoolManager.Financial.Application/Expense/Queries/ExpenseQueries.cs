using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record GetExpenseByIdQuery(Guid Id) : IQuery<ExpenseDto?>;

    /// <param name="AcademicYearId">Only that year's expenses; null = all</param>
    public record GetAllExpensesQuery(Guid? AcademicYearId = null) : IQuery<List<ExpenseDto>>;
}
