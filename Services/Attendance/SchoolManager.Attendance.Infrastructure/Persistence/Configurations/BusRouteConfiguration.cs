using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class BusRouteConfiguration : IEntityTypeConfiguration<BusRoute>
    {
        public void Configure(EntityTypeBuilder<BusRoute> builder)
        {
            builder.ToTable("BusRoutes");
            builder.HasKey(x => x.Id);

            builder.ConfigureLocalized(x => x.Name, "Name");
            builder.Property(x => x.Description).HasMaxLength(BusRoute.MaxDescriptionLength);

            builder.HasMany(x => x.Stops).WithOne().HasForeignKey(s => s.BusRouteId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.Stops).HasField("_stops").UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
