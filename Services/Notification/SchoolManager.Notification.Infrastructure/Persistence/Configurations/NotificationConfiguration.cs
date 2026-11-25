using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Domain.Notification>
    {
        public void Configure(EntityTypeBuilder<Domain.Notification> builder)
        {
            builder.HasIndex(x => x.AcademicYearId);
            builder.ToTable("Notifications");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).IsRequired().HasMaxLength(2000);
            builder.Property(x => x.ImageUrl).HasMaxLength(500);

            builder.Property(x => x.Source).HasDefaultValue(NotificationSource.Admin);
            // One notification per integration event (Kafka may deliver a message twice)
            builder.HasIndex(x => x.SourceEventId).IsUnique().HasFilter("[SourceEventId] IS NOT NULL");

            // The push job looks for due notifications not pushed yet
            builder.HasIndex(x => new { x.PushSentAt, x.SentOn });

            builder.HasMany(x => x.Targets)
                .WithOne()
                .HasForeignKey(t => t.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
