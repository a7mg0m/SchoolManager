using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class EventTargetConfiguration : IEntityTypeConfiguration<EventTarget>
    {
        public void Configure(EntityTypeBuilder<EventTarget> builder)
        {
            builder.ToTable("EventTargets");
            builder.HasKey(x => x.Id);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
