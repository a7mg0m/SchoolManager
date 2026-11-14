using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// Menu items as a tree (top-level items with their children, ordered).
    /// ActiveOnly leaves out inactive items and everything under them (use it for the sidebar).
    /// </summary>
    public record GetMenuTreeQuery(bool ActiveOnly) : IQuery<List<MenuItemTreeDto>>;
}
