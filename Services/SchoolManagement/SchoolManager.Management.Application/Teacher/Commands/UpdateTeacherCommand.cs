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
        bool IsActive,
        string? LoginName = null,
        string? Password = null
    ) : ICommand<bool>;
}
