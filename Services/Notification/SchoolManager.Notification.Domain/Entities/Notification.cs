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
            AuditInfo auditInfo)
        {
            Title = title;
            Description = description;
            SentOn = sentOn;
            NotificationType = notificationType;
            ImageUrl = imageUrl;
            AuditInfo = auditInfo;
        }

        public void Update(
            string title,
            string description,
            DateOnly sentOn,
            NotificationTypeEnum notificationType,
            string? imageUrl,
            AuditInfo auditInfo)
        {
            Title = title;
            Description = description;
            SentOn = sentOn;
            NotificationType = notificationType;
            ImageUrl = imageUrl;
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

          