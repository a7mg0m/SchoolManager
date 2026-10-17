using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SchoolManager.Notification.Infrastructure
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Domain.Notification>
    {
        public void Configure(EntityTypeBuilder<Domain.Notification> builder)
        {
            builder.ToTable("Notifications");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).IsRequired().HasMaxLength(2000);
            builder.Property(x => x.ImageUrl).HasMaxLength(500);

            builder.HasMany(x => x.Targets)
                .WithOne()
                .HasForeignKey(t => t.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
