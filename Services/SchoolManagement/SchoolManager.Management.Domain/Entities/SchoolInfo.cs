using BuildingBlocks;

namespace SchoolManager.Management.Domain
{
    public sealed class SchoolInfo : BaseEntity
    {
        public NameLocalized Name { get; private set; }
        public string? Address { get; private set; }
        public string? Phone { get; private set; }
        public string? Email { get; private set; }
        public byte[]? Logo { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        private SchoolInfo() { }

        public SchoolInfo(NameLocalized name, string? address, string? phone, string? email, byte[]? logo, AuditInfo auditInfo)
        {
            Name = name;
            Address = address;
            Phone = phone;
            Email = email;
            Logo = logo;
            AuditInfo = auditInfo;
        }

        public void Update(NameLocalized name, string? address, string? phone, string? email, byte[]? logo, AuditInfo auditInfo)
        {
            Name = name;
            Address = address;
            Phone = phone;
            Email = email;
            Logo = logo;
            AuditInfo = auditInfo;
        }
    }
}
