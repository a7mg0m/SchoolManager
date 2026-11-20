using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class PeriodAttendanceConfiguration : IEntityTypeConfiguration<PeriodAttendance>
    {
        public void Configure(EntityTypeBuilder<PeriodAttendance> builder)
        {
            builder.ToTable("PeriodAttendances");
            builder.HasKey(x => x.Id);
            // Ids come from the entity: periods added to a day's list are inserted (not taken as existing rows)
            builder.Property(x => x.Id).ValueGeneratedNever();

            // Ids of the Management service: plain columns, no foreign key
            builder.Property(x => x.TimetableSlotId).IsRequired();
            builder.Property(x => x.SubjectId).IsRequired();
            builder.Property(x => x.TeacherId).IsRequired();

            builder.Property(x => x.StartTime).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.AbsenceReason).HasMaxLength(PeriodAttendance.MaxAbsenceReasonLength);
            builder.Property(x => x.RecordedAt).IsRequired();

            // A lesson is recorded once per day
            builder.HasIndex(x => new { x.DailyAttendanceId, x.TimetableSlotId }).IsUnique();
            builder.HasIndex(x => x.TimetableSlotId);
            builder.HasIndex(x => x.SubjectId);
            builder.HasIndex(x => x.TeacherId);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
