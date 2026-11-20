using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class TripStudentConfiguration : IEntityTypeConfiguration<TripStudent>
    {
        public void Configure(EntityTypeBuilder<TripStudent> builder)
        {
            builder.ToTable("TripStudents");
            builder.HasKey(x => x.Id);
            // Ids come from the entity: students added to a trip's list are inserted (not taken as existing rows)
            builder.Property(x => x.Id).ValueGeneratedNever();

            // Id of the Management service: plain column, no foreign key
            builder.Property(x => x.StudentId).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);

            // A student is on a trip once
            builder.HasIndex(x => new { x.TripId, x.StudentId }).IsUnique();
            builder.HasIndex(x => x.StudentId);
        }
    }
}
