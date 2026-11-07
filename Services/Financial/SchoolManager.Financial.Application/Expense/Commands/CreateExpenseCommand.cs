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
        Guid CreatedBy
    ) : ICommand<Guid>;
}
