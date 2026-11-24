using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class BusRouteStopConfiguration : IEntityTypeConfiguration<BusRouteStop>
    {
        public void Configure(EntityTypeBuilder<BusRouteStop> builder)
        {
            builder.ToTable("BusRouteStops");
            builder.HasKey(x => x.Id);
            // Ids come from the entity: stops added to a route's list are inserted (not taken as existing rows)
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.ConfigureLocalized(x => x.Name, "Name");
            builder.Property(x => x.Order).IsRequired();

            // The order of a stop is unique within its route
            builder.HasIndex(x => new { x.BusRouteId, x.Order }).IsUnique();
        }
    }
}
