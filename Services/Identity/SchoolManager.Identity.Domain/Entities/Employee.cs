using BuildingBlocks;

namespace SchoolManager.Identity.Domain
{
    /// <summary>A school employee (non-teaching staff). Optionally linked to a login <see cref="User"/>.</summary>
    public class Employee : BaseEntity
    {
        public NameLocalized FullName { get; private set; }
        public string NationalId { get; private set; }
        public string PhoneNumber { get; private set; }
        public string? Email { get; private set; }
        public string? Address { get; private set; }
        public string JobTitle { get; private set; }
        public DateOnly HireDate { get; private set; }
        public bool IsActive { get; private set; }

        public Guid EmployeeGroupId { get; private set; }
        public EmployeeGroup? EmployeeGroup { get; private set; }

        /// <summary>Login account of the employee, if they can sign in.</summary>
        public Guid? UserId { get; private set; }
        public User? User { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        private Employee()
        {
        }

        public Employee(
            NameLocalized fullName,
            string nationalId,
            string phoneNumber,
            string? email,
            string? address,
            string jobTitle,
            DateOnly hireDate,
            Guid employeeGroupId,
            Guid? userId,
            bool isActive,
            AuditInfo auditInfo)
        {
            FullName = fullName;
            NationalId = nationalId;
            PhoneNumber = phoneNumber;
            Email = email;
            Address = address;
            JobTitle = jobTitle;
            HireDate = hireDate;
            EmployeeGroupId = employeeGroupId;
            UserId = userId;
            IsActive = isActive;
            AuditInfo = auditInfo;
        }

        public void Update(
            NameLocalized fullName,
            string nationalId,
            string phoneNumber,
            string? email,
            string? address,
            string jobTitle,
            DateOnly hireDate,
            Guid employeeGroupId,
            Guid? userId,
            AuditInfo auditInfo)
        {
            FullName = fullName;
            NationalId = nationalId;
            PhoneNumber = phoneNumber;
            Email = email;
            Address = address;
            JobTitle = jobTitle;
            HireDate = hireDate;
            EmployeeGroupId = employeeGroupId;
            UserId = userId;
            AuditInfo = auditInfo;
        }

        public void Activate(AuditInfo auditInfo)
        {
            IsActive = true;
            AuditInfo = auditInfo;
        }

        public void Deactivate(AuditInfo auditInfo)
        {
            IsActive = false;
            AuditInfo = auditInfo;
        }
    }
}
