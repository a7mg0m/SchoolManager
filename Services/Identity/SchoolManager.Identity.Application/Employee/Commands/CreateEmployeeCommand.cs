using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>Creates the employee together with its User (role Admin) and UserProfile.</summary>
    public record CreateEmployeeCommand(
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
        string Password,
        bool IsActive
    ) : ICommand<Guid>;
}
