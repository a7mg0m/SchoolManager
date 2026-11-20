using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Notification.Domain
{
    public sealed class Notification : BaseEntity
    {
        public string Title { get; private set; }
        public string Description { get; private set; }

        public DateOnly SentOn { get; private set; }
      
        public NotificationTypeEnum NotificationType { get; private set; }

        /// <summary>Academic year the notification belongs to (null: saved before notifications had a year)</summary>
        public Guid? AcademicYearId { get; private set; }

        public string? ImageUrl { get; private set; } // ØµÙˆØ±Ø© Ø§Ù„Ø­Ø¯Ø«
        public AuditInfo AuditInfo { get; private set; }

        public List<NotificationTarget> Targets { get; private set; } = new();

        private Notification() { }

        public Notification(
            string title,
            string description,
            DateOnly sentOn,
            NotificationTypeEnum notificationType,
            string? imageUrl,
            AuditInfo auditInfo,
            Guid? academicYearId = null)
        {
            Title = title;
            Description = description;
            SentOn = sentOn;
            NotificationType = notificationType;
            ImageUrl = imageUrl;
            AcademicYearId = academicYearId;
            AuditInfo = auditInfo;
        }

        public void Update(
            string title,
            string description,
            DateOnly sentOn,
            NotificationTypeEnum notificationType,
            string? imageUrl,
            AuditInfo auditInfo,
            Guid? academicYearId = null)
        {
            Title = title;
            Description = description;
            SentOn = sentOn;
            NotificationType = notificationType;
            ImageUrl = imageUrl;
            // A year is kept once set (an edit without one does not clear it)
            AcademicYearId = academicYearId ?? AcademicYearId;
            AuditInfo = auditInfo;
        }

        public void AddTarget(Guid? gradeId, Guid? sectionId, Guid? teacherId, Guid? studentId, AuditInfo auditInfo)
        {
            Targets.Add(new NotificationTarget(Id, gradeId, sectionId, teacherId, studentId, auditInfo));
        }

        public void ClearTargets()
        {
            Targets.Clear();
        }
    }
}

          