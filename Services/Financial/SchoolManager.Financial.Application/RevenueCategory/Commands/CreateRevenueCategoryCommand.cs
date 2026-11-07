using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record CreateRevenueCategoryCommand(
        string Name,
        string? Description,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
