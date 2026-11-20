using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public record CreateExpenseCommand(
        string Title,
        string? Description,
        Guid EventId,
        Guid CategoryId,
        ExpenseType Type,
        decimal Amount,
        string Currency,
        DateTime ExpenseDate,
        string? Reference,
        /// <summary>The academic year chosen in the admin header</summary>
        Guid AcademicYearId = default
    ) : ICommand<Guid>;
}
