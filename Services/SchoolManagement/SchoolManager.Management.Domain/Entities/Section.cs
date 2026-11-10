using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Management.Domain
{
    public sealed class Section : BaseEntity
    {
        public NameLocalized Name { get; private set; }
        public Guid GradeId { get; private set; }
        public Guid StageId { get; private set; }
        public bool IsActive { get; private set; }
        public AuditInfo AuditInfo { get; private set; }

        public Grade? Grade { get; private set; }
        public Stage? Stage { get; private set; }
        public List<Student> Students { get; private set; } = new();

        private Section() { }

        public Section(NameLocalized name, Guid gradeId, Guid stageId, AuditInfo auditInfo)
        {
            Name = name;
            GradeId = gradeId;
            StageId = stageId;
            IsActive = true;
            AuditInfo = auditInfo;
        }

        public void Update(NameLocalized name, Guid gradeId, Guid stageId, AuditInfo auditInfo)
        {
            Name = name;
            GradeId = gradeId;
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
