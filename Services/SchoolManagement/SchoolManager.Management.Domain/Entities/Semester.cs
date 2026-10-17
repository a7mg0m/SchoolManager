using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Management.Domain
{
    public sealed class Semester : BaseEntity
    {
        public string Name { get; private set; } // مثال: الفصل الأول
        public Guid AcademicYearId { get; private set; }
        public bool IsActive { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        public AcademicYear? AcademicYear { get; private set; }

        private Semester() { }

        public Semester(string name, Guid academicYearId, AuditInfo auditInfo)
        {
            Name = name;
            AcademicYearId = academicYearId;
            IsActive = true;

            AuditInfo = auditInfo;
        }

        public void Update(string name, Guid academicYearId, AuditInfo auditInfo)
        {
            Name = name;
            AcademicYearId = academicYearId;
            AuditInfo = auditInfo;
        }

        public void Deactivate(AuditInfo auditInfo)
        {
            IsActive = false;
            AuditInfo = auditInfo;
        }
    }

}
