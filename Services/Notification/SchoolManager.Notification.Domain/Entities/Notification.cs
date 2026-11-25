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

        /// <summary>When the push notification went to the recipients' phones (UTC); null = not pushed yet (e.g. SentOn in the future)</summary>
        public DateTime? PushSentAt { get; private set; }

        /// <summary>Admin (the admin page) or a system notification (e.g. a homework published by a teacher)</summary>
        public NotificationSource Source { get; private set; } = NotificationSource.Admin;

        /// <summary>The integration event a system notification was created from (one notification per event)</summary>
        public Guid? SourceEventId { get; private set; }

        /// <summary>Marks a notification created by the system from an integration event</summary>
        public void MarkSystem(NotificationSource source, Guid eventId)
        {
            Source = source;
            SourceEventId = eventId;
        }

        public List<NotificationTarget> Targets { get; private set; } = new();

        public void MarkPushSent(DateTime at) => PushSentAt = at;

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

        public void AddTarget(Guid? gradeId, Guid? sectionId, Guid? teacherId, Guid? studentId, AuditInfo auditInfo, Guid? busId = null)
        {
            Targets.Add(new NotificationTarget(Id, gradeId, sectionId, teacherId, studentId, auditInfo, busId));
        }

        public void ClearTargets()
        {
            Targets.Clear();
        }
    }
}

          