using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record UpdateRevenueCategoryCommand(
        Guid Id,
        string Name,
        string? Description,
        Guid UpdatedBy
    ) : ICommand<bool>;
}
