using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record CreateExpenseCategoryCommand(
        string Name,
        string? Description
    ) : ICommand<Guid>;
}
