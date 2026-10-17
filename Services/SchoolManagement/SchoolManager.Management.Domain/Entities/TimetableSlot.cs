using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Management.Domain
{
    public sealed class TimetableSlot : BaseEntity
    {
        public DayOfWeek Day { get; private set; }
        public NameLocalized Name { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public TimeOnly EndTime { get; private set; }

        public Guid SubjectId { get; private set; }
        public Guid TeacherId { get; private set; }
        public Guid GradeId { get; private set; }
        public Guid SectionId { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        // Navigation
        public Subject? Subject { get; private set; }
        public Teacher? Teacher { get; private set; }
        public Grade? Grade { get; private set; }
        public Section? Section { get; private set; }

        private TimetableSlot() { }

        public TimetableSlot(
            DayOfWeek day,
            NameLocalized name,
            TimeOnly startTime,
            TimeOnly endTime,
            Guid subjectId,
            Guid teacherId,
            Guid gradeId,
            Guid sectionId,
            AuditInfo auditInfo)
        {
            Day = day;
            Name = name;
            StartTime = startTime;
            EndTime = endTime;
            SubjectId = subjectId;
            TeacherId = teacherId;
            GradeId = gradeId;
            SectionId = sectionId;

            AuditInfo = auditInfo;
        }

        public void Update(
            DayOfWeek day,
            NameLocalized name,
            TimeOnly startTime,
            TimeOnly endTime,
            Guid gradeId,
            Guid sectionId,
            Guid subjectId,
            Guid teacherId,
            AuditInfo auditInfo)
        {
            Day = day;
            Name = name;
            GradeId = gradeId;
            SectionId = sectionId;
            StartTime = startTime;
            EndTime = endTime;
            SubjectId = subjectId;
            TeacherId = teacherId;
            AuditInfo = auditInfo;
        }
    }

}
