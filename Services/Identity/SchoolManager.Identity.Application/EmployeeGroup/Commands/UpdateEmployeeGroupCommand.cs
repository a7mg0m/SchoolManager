using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record UpdateEmployeeGroupCommand(
        Guid Id,
        string NameAr,
        string NameEn,
        string? Description
    ) : ICommand<bool>;
}
