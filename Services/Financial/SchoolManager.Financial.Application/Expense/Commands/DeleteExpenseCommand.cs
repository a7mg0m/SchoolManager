using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record DeleteExpenseCommand(Guid Id) : ICommand<bool>;
}
