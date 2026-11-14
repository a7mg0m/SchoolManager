using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record CreateMenuItemCommand(
        string NameAr,
        string NameEn,
        string? Icon,
        string? Route,
        int? Order,
        Guid? ParentId,
        bool IsActive
    ) : ICommand<Guid>;
}
