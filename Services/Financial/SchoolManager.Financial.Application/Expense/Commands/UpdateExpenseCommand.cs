using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public record UpdateExpenseCommand(
        Guid Id,
        string Title,
        string? Description,
        Guid EventId,
        Guid CategoryId,
        ExpenseType Type,
        decimal Amount,
        string Currency,
        DateTime ExpenseDate,
        string? Reference,
        /// <summary>Only used when the expense has no year yet</summary>
        Guid? AcademicYearId = null
    ) : ICommand<bool>;
}
