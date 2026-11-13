using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record UpdateExpenseCategoryCommand(
        Guid Id,
        string Name,
        string? Description
    ) : ICommand<bool>;
}
