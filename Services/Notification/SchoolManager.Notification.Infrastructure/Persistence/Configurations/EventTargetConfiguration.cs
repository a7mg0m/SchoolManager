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
            // Ids are created in BaseEntity's constructor. Without this, EF treats a new target added to a
            // tracked event (update: ClearTargets + AddTarget) as an existing row and issues an UPDATE that
            // affects 0 rows -> DbUpdateConcurrencyException.
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
