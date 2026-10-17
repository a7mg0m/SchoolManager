using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateTeacherCommand(
        Guid Id,
        string FullNameAr,
        string FullNameEn,
        string PhoneNumber,
        string? Email,
        string Address,
        Guid UpdatedBy
    ) : ICommand<bool>;
}
