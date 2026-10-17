using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace SchoolManager.Management.Domain
{
    public sealed class Grade : BaseEntity
    {
        public NameLocalized Name { get; private set; }
        public Guid StageId { get; private set; }
        public bool IsActive { get; private set; }

        public AuditInfo AuditInfo { get; private set; }
        public Stage? Stage { get; private set; }
        public AcademicYear? AcademicYear { get; private set; }
        public List<Section> Sections { get; private set; } = new();

        private Grade() { }

        public Grade(NameLocalized name, Guid stageId, AuditInfo auditInfo)
        {
            Name = name;
            StageId = stageId;
            IsActive = true;
            AuditInfo = auditInfo;
        }

        public void Update(NameLocalized name,Guid stageId, AuditInfo auditInfo)
        {
            Name = name;
            StageId = stageId;
            AuditInfo = auditInfo;
        }

        public void Deactivate(AuditInfo auditInfo)
        {
            IsActive = false;
            AuditInfo = auditInfo;
        }
    }

}
