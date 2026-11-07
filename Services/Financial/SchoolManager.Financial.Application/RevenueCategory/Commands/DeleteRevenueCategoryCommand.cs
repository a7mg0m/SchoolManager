using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record DeleteRevenueCategoryCommand(Guid Id) : ICommand<bool>;
}
