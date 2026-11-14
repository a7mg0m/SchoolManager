using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    /// <summary>All menu items as a flat list (ordered), including inactive ones.</summary>
    public record GetAllMenuItemsQuery() : IQuery<List<MenuItemDto>>;
}
