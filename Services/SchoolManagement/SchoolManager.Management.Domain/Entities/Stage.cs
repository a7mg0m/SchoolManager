using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Management.Domain
{
    public sealed class Stage : BaseEntity
    {
        public NameLocalized Name { get; private set; }
        public bool IsActive { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        public List<Grade> Grades { get; private set; } = new();

        private Stage() { }

        public Stage(NameLocalized name, AuditInfo auditInfo)
        {
            Name = name;
            IsActive = true;

            AuditInfo = auditInfo;
        }

        public void Update(NameLocalized name, AuditInfo auditInfo)
        {
            Name = name;

            AuditInfo = auditInfo;
        }

        public void Deactivate(AuditInfo auditInfo)
        {
            IsActive = false;
            AuditInfo = auditInfo;
        }
    }

}
