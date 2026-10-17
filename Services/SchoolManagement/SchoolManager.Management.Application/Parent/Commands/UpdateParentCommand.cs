using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateParentCommand(
        Guid Id,
        string FullNameAr,
        string FullNameEn,
        string PhoneNumber,
        string? Email,
        string Address,
        Guid UpdatedBy
    ) : ICommand<bool>;
}
