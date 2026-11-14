using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>Updating an employee also updates its User and UserProfile. Leave Password empty to keep it.</summary>
    public class UpdateEmployeeDto
    {
        public Guid Id { get; set; }
        public string FullNameAr { get; set; } = string.Empty;
        public string FullNameEn { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public DateOnly HireDate { get; set; }
        public Guid EmployeeGroupId { get; set; }
        public string LoginName { get; set; } = string.Empty;
        public string? Password { get; set; }
    }
}
