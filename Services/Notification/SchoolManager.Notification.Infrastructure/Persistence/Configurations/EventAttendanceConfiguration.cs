using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class EventAttendanceConfiguration : IEntityTypeConfiguration<EventAttendance>
    {
        public void Configure(EntityTypeBuilder<EventAttendance> builder)
        {
            builder.ToTable("EventAttendances");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.AttendeeType).HasConversion<int>();

            // One mark per event and person
            builder.HasIndex(x => new { x.EventId, x.AttendeeType, x.AttendeeId }).IsUnique();

            // Deleting the event deletes its attendance
            builder.HasOne<SchoolEvent>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
