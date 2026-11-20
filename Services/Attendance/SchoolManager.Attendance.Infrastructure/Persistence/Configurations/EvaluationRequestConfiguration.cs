using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class EvaluationRequestConfiguration : IEntityTypeConfiguration<EvaluationRequest>
    {
        public void Configure(EntityTypeBuilder<EvaluationRequest> builder)
        {
            builder.ToTable("EvaluationRequests");
            builder.HasKey(x => x.Id);

            // Ids of other services: plain columns, no foreign key
            builder.Property(x => x.StudentId).IsRequired();
            builder.Property(x => x.ParentId).IsRequired();
            builder.Property(x => x.TeacherId).IsRequired();

            builder.Property(x => x.RequestedAt).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(EvaluationRequest.MaxNoteLength);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);

            // Teacher: pending requests; parent app: the parent's requests
            builder.HasIndex(x => new { x.TeacherId, x.Status });
            builder.HasIndex(x => x.ParentId);
            builder.HasIndex(x => x.StudentId);
            builder.HasIndex(x => x.SubjectId);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
