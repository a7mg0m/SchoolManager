using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record CreateExpenseCategoryCommand(
        string Name,
        string? Description,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
