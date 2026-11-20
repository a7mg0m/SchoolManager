using BuildingBlocks;

namespace SchoolManager.Management.Domain
{
    public sealed class Vaccine : BaseEntity
    {
        public NameLocalized Name { get; private set; }
        public int TotalDoses { get; private set; }
        public bool IsMandatory { get; private set; }
        public bool IsActive { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        private Vaccine() { }

        public Vaccine(NameLocalized name, int totalDoses, bool isMandatory, AuditInfo auditInfo)
        {
            Name = name;
            TotalDoses = totalDoses;
            IsMandatory = isMandatory;
            IsActive = true;
            AuditInfo = auditInfo;
        }

        public void Update(NameLocalized name, int totalDoses, bool isMandatory, AuditInfo auditInfo)
        {
            Name = name;
            TotalDoses = totalDoses;
            IsMandatory = isMandatory;
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
