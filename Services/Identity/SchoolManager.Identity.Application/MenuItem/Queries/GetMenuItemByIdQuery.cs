using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record GetMenuItemByIdQuery(Guid Id) : IQuery<MenuItemDto?>;
}
