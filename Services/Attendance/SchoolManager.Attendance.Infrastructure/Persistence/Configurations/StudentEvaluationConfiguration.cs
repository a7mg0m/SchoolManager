using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class StudentEvaluationConfiguration : IEntityTypeConfiguration<StudentEvaluation>
    {
        public void Configure(EntityTypeBuilder<StudentEvaluation> builder)
        {
            builder.ToTable("StudentEvaluations");
            builder.HasKey(x => x.Id);

            // Ids of other services: plain columns, no foreign key
            builder.Property(x => x.StudentId).IsRequired();
            builder.Property(x => x.TeacherId).IsRequired();
            builder.Property(x => x.SubjectId).IsRequired();
            builder.Property(x => x.TimetableSlotId).IsRequired();

            builder.Property(x => x.Date).IsRequired();
            builder.Property(x => x.Focus).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.Interaction).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.Behavior).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.HealthStatus).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.Comment).HasMaxLength(StudentEvaluation.MaxCommentLength);

            // One evaluation per student, lesson and day
            builder.HasIndex(x => new { x.StudentId, x.TimetableSlotId, x.Date }).IsUnique();
            builder.HasIndex(x => x.Date);
            builder.HasIndex(x => x.TeacherId);
            builder.HasIndex(x => x.SubjectId);
            builder.HasIndex(x => x.TimetableSlotId);
            builder.HasIndex(x => x.EvaluationRequestId);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
