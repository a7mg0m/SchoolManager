using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Management.Domain
{
    public sealed class Timetable : BaseEntity
    {
        public Guid GradeId { get; private set; }
        public Guid AcademicYearId { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        // Navigation
        public Grade? Grade { get; private set; }
        public AcademicYear? AcademicYear { get; private set; }
        public List<TimetableSlot> Slots { get; private set; } = new();

        private Timetable() { }

        public Timetable(Guid gradeId, Guid academicYearId, AuditInfo auditInfo)
        {
            GradeId = gradeId;
            AcademicYearId = academicYearId;

            AuditInfo = auditInfo;
        }

        public void AddSlot(TimetableSlot slot)
        {
            Slots.Add(slot);
        }

        public void Update(Guid gradeId, Guid academicYearId,AuditInfo auditInfo)
        {
            GradeId = gradeId;
            AcademicYearId = academicYearId;
            AuditInfo = auditInfo;
        }
               
    }

}
