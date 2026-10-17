using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Notification.Domain
{
    public sealed class SchoolEvent : BaseEntity
    {
        public string Title { get; private set; }
        public string Description { get; private set; }

        public DateOnly EventDate { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public TimeOnly EndTime { get; private set; }
        public EventForEnum EventFor { get; private set; }

        public string? ImageUrl { get; private set; } // صورة الحدث

        public Guid AcademicYearId { get; private set; }
        public Guid SemesterId { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        public List<EventTarget> Targets { get; private set; } = new();

        private SchoolEvent() { }

        public SchoolEvent(
            string title,
            string description,
            DateOnly eventDate,
            TimeOnly startTime,
            TimeOnly endTime,
            string? imageUrl,
            EventForEnum eventFor,
            Guid academicYearId,
            Guid semesterId,
            AuditInfo auditInfo)
        {
            Title = title;
            Description = description;
            EventDate = eventDate;
            StartTime = startTime;
            EndTime = endTime;
            ImageUrl = imageUrl;
            AcademicYearId = academicYearId;
            SemesterId = semesterId;
            
            EventFor = eventFor;
            AuditInfo = auditInfo;
        }

        public void Update(
           string title,
            string description,
            DateOnly eventDate,
            TimeOnly startTime,
            TimeOnly endTime,
            string? imageUrl,
            EventForEnum eventFor,
            Guid academicYearId,
            Guid semesterId,
            AuditInfo auditInfo)
        {
            Title = title;
            Description = description;
            EventDate = eventDate;
            StartTime = startTime;
            EndTime = endTime;
            ImageUrl = imageUrl;
            AcademicYearId = academicYearId;
            SemesterId = semesterId;

            EventFor = eventFor;
            AuditInfo = auditInfo;
        }
        public void AddTarget(Guid eventId, Guid? gradeId, Guid? sectionId, Guid? teacherId, Guid? studentId, AuditInfo auditInfo)
        {
            Targets.Add(new EventTarget(eventId, gradeId, sectionId, teacherId, studentId, auditInfo));
        }

        public void ClearTargets()
        {
            Targets.Clear();
        }
    }

}
