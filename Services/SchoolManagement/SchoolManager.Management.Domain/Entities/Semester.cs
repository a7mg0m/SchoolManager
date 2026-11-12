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
        public DateOnly StartDate { get; private set; }
        public DateOnly EndDate { get; private set; }
        public bool IsActive { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        public AcademicYear? AcademicYear { get; private set; }

        private Semester() { }

        public Semester(string name, Guid academicYearId, DateOnly startDate, DateOnly endDate, AuditInfo auditInfo)
        {
            Name = name;
            AcademicYearId = academicYearId;
            StartDate = startDate;
            EndDate = endDate;
            IsActive = true;

            AuditInfo = auditInfo.Copy(); // copy: owned type, the caller may share one instance across semesters
        }

        public void Update(string name, Guid academicYearId, DateOnly startDate, DateOnly endDate, AuditInfo auditInfo)
        {
            Name = name;
            AcademicYearId = academicYearId;
            StartDate = startDate;
            EndDate = endDate;
            AuditInfo = auditInfo.Copy(); // copy: owned type, the caller may share one instance across semesters
        }

        public void Deactivate(AuditInfo auditInfo)
        {
            IsActive = false;
            AuditInfo = auditInfo.Copy(); // copy: owned type, the caller may share one instance across semesters
        }
    }

}
