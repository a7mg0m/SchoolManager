using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Management.Domain
{
    public sealed class AcademicYear : BaseEntity
    {
        public string Name { get; private set; } // مثال: 2024-2025
        public DateOnly StartDate { get; private set; }
        public DateOnly EndDate { get; private set; }
        public bool IsActive { get; private set; }

        public AuditInfo AuditInfo { get; private set; }
        public List<Semester> Semesters { get; private set; } = new();

        private AcademicYear() { }

        public AcademicYear(
            string name,
            DateOnly startDate,
            DateOnly endDate,
            AuditInfo auditInfo)
        {
            Name = name;
            StartDate = startDate;
            EndDate = endDate;
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
