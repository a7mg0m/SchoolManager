using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class TripConfiguration : IEntityTypeConfiguration<Trip>
    {
        public void Configure(EntityTypeBuilder<Trip> builder)
        {
            builder.ToTable("Trips");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Date).IsRequired();
            builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.CurrentLatitude);
            builder.Property(x => x.CurrentLongitude);

            builder.HasOne<Bus>().WithMany().HasForeignKey(x => x.BusId).OnDelete(DeleteBehavior.Restrict);

            // One trip of each type per bus per day; today's running trips
            builder.HasIndex(x => new { x.BusId, x.Date, x.Type }).IsUnique();
            builder.HasIndex(x => new { x.Date, x.Status });

            builder.HasMany(x => x.Students).WithOne().HasForeignKey(s => s.TripId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.Students).HasField("_students").UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
