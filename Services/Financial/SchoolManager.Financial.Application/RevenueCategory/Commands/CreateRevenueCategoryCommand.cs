using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record CreateRevenueCategoryCommand(
        string Name,
        string? Description
    ) : ICommand<Guid>;
}
