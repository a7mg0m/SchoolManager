using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class DailyAttendanceConfiguration : IEntityTypeConfiguration<DailyAttendance>
    {
        public void Configure(EntityTypeBuilder<DailyAttendance> builder)
        {
            builder.ToTable("DailyAttendances");
            builder.HasKey(x => x.Id);

            // Ids of other services: plain columns, no foreign key
            builder.Property(x => x.StudentId).IsRequired();
            builder.Property(x => x.SectionId).IsRequired();
            builder.Property(x => x.GradeId).IsRequired();
            builder.Property(x => x.AcademicYearId).IsRequired();

            builder.Property(x => x.Date).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.Notes).HasMaxLength(DailyAttendance.MaxNotesLength);

            // Derived from the periods (not stored)
            builder.Ignore(x => x.AttendedPeriodsCount);
            builder.Ignore(x => x.AbsentPeriodsCount);

            // One row per student per day
            builder.HasIndex(x => new { x.StudentId, x.Date }).IsUnique();
            builder.HasIndex(x => new { x.SectionId, x.Date });
            builder.HasIndex(x => x.GradeId);
            builder.HasIndex(x => x.AcademicYearId);

            builder.HasMany(x => x.Periods).WithOne().HasForeignKey(p => p.DailyAttendanceId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.Periods).HasField("_periods").UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
