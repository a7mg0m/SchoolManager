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
        string? Password = null,
        string? JobLevel = null,
        bool IsServiceEnded = false,
        DateOnly? ServiceEndDate = null,
        string? JobTitle = null,
        string? EmployeeNumber = null,
        string? Qualification = null,
        string? Specialization = null,
        DateOnly? HireDate = null,
        int? YearsOfExperience = null
    ) : ICommand<bool>;
}
