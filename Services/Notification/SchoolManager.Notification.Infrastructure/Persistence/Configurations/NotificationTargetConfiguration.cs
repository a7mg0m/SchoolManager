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
            // Same as EventTargetConfiguration: ids come from BaseEntity, so new targets added on update
            // must be inserted, not treated as existing rows.
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
