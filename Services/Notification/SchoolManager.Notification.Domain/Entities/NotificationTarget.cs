using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Notification.Domain
{
    public sealed class NotificationTarget : BaseEntity
    {
        public Guid NotificationId { get; private set; }
        public Guid? GradeId { get; private set; }
        public Guid? SectionId { get; private set; }
        public Guid? TeacherId { get; private set; }
        public Guid? StudentId { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        private NotificationTarget() { }

        public NotificationTarget(Guid notificationId, Guid? gradeId, Guid? sectionId, Guid? teacherId, Guid? studentId, AuditInfo auditInfo)
        {
            NotificationId = notificationId;
            GradeId = gradeId;
            SectionId = sectionId;
            TeacherId = teacherId;
            StudentId = studentId;

            AuditInfo = auditInfo;
        }
    }

}
