using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>Updates the employee and keeps its User and UserProfile in sync. Empty Password keeps the current one.</summary>
    public record UpdateEmployeeCommand(
        Guid Id,
        string FullNameAr,
        string FullNameEn,
        string NationalId,
        string PhoneNumber,
        string Email,
        string? Address,
        string JobTitle,
        DateOnly HireDate,
        Guid EmployeeGroupId,
        string LoginName,
        string? Password
    ) : ICommand<bool>;
}
