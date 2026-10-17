using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class SchoolEventConfiguration : IEntityTypeConfiguration<SchoolEvent>
    {
        public void Configure(EntityTypeBuilder<SchoolEvent> builder)
        {
            builder.ToTable("SchoolEvents");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).IsRequired().HasMaxLength(2000);
            builder.Property(x => x.ImageUrl).HasMaxLength(500);

            builder.HasMany(x => x.Targets)
                .WithOne()
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
