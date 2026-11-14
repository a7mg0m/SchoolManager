using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record UpdateMenuItemCommand(
        Guid Id,
        string NameAr,
        string NameEn,
        string? Icon,
        string? Route,
        int Order,
        Guid? ParentId
    ) : ICommand<bool>;
}
