using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    /// <summary>Fails with <see cref="ErrorKeys.MenuItemHasChildren"/> while the item has child items.</summary>
    public record DeleteMenuItemCommand(Guid Id) : ICommand<bool>;
}
