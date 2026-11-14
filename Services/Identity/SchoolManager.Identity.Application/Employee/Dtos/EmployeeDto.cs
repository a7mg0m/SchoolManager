namespace SchoolManager.Identity.Application
{
    public class EmployeeDto
    {
        public Guid Id { get; set; }
        public string FullNameAr { get; set; } = string.Empty;
        public string FullNameEn { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public DateOnly HireDate { get; set; }
        public Guid EmployeeGroupId { get; set; }
        public string? EmployeeGroupNameAr { get; set; }
        public string? EmployeeGroupNameEn { get; set; }
        /// <summary>Login account created for the employee (role Admin).</summary>
        public Guid? UserId { get; set; }
        public string? LoginName { get; set; }
        public bool IsActive { get; set; }
    }
}
