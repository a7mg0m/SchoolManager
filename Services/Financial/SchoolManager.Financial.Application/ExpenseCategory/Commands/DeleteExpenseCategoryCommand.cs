using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record DeleteExpenseCategoryCommand(Guid Id) : ICommand<bool>;
}
