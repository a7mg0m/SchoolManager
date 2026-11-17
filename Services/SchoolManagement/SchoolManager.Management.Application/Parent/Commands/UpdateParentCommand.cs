using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <param name="LoginName">Login name of the parent's account (required).</param>
    /// <param name="Password">New password, or null / empty to keep the current one; only its hash leaves this service.</param>
    public record UpdateParentCommand(
        Guid Id,
        string FullNameAr,
        string FullNameEn,
        string PhoneNumber,
        string? Email,
        string Address,
        List<ParentStudentDto> Students,
        string? LoginName,
        string? Password
    ) : ICommand<bool>;
}
