using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    public record CreateEmployeeGroupCommand(
        string NameAr,
        string NameEn,
        string? Description,
        bool IsActive
    ) : ICommand<Guid>;
}
