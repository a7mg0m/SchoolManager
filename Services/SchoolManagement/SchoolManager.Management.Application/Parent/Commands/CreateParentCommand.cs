using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <param name="LoginName">Login name of the parent's account (required).</param>
    /// <param name="Password">First password of the account (required); only its hash leaves this service.</param>
    public record CreateParentCommand(
        string FullNameAr,
        string FullNameEn,
        string PhoneNumber,
        string? Email,
        string Address,
        List<ParentStudentDto> Students,
        string? LoginName,
        string? Password
    ) : ICommand<Guid>;
}
