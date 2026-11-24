using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class BusConfiguration : IEntityTypeConfiguration<Bus>
    {
        public void Configure(EntityTypeBuilder<Bus> builder)
        {
            builder.ToTable("Buses");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Number).IsRequired().HasMaxLength(50);
            builder.Property(x => x.PlateNumber).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Capacity).IsRequired();
            builder.Property(x => x.DriverName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.DriverPhone).IsRequired().HasMaxLength(20);

            builder.Property(x => x.DriverLoginName).HasMaxLength(100);

            // One bus per plate number
            builder.HasIndex(x => x.PlateNumber).IsUnique();

            // One driver login per name
            builder.HasIndex(x => x.DriverLoginName).IsUnique().HasFilter("[DriverLoginName] IS NOT NULL");

            builder.HasOne<BusRoute>().WithMany().HasForeignKey(x => x.BusRouteId).OnDelete(DeleteBehavior.Restrict);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
