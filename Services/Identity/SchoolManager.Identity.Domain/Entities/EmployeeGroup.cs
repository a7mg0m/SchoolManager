using BuildingBlocks;

namespace SchoolManager.Identity.Domain
{
    /// <summary>A group of employees, e.g. Administration, Accounting, Security, Cleaning.</summary>
    public class EmployeeGroup : BaseEntity
    {
        public NameLocalized Name { get; private set; }
        public string? Description { get; private set; }
        public bool IsActive { get; private set; }
        public AuditInfo AuditInfo { get; private set; }

        public List<Employee> Employees { get; private set; } = new();

        private EmployeeGroup()
        {
        }

        public EmployeeGroup(NameLocalized name, string? description, bool isActive, AuditInfo auditInfo)
        {
            Name = name;
            Description = description;
            IsActive = isActive;
            AuditInfo = auditInfo;
        }

        public void Update(NameLocalized name, string? description, AuditInfo auditInfo)
        {
            Name = name;
            Description = description;
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
