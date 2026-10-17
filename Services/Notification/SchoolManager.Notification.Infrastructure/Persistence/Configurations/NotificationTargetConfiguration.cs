using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class NotificationTargetConfiguration : IEntityTypeConfiguration<NotificationTarget>
    {
        public void Configure(EntityTypeBuilder<NotificationTarget> builder)
        {
            builder.ToTable("NotificationTargets");
            builder.HasKey(x => x.Id);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
