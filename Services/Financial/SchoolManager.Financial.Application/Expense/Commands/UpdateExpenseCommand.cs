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
        Guid UpdatedBy
    ) : ICommand<bool>;
}
