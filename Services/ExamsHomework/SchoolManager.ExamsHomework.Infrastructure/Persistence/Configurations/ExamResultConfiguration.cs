using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class ExamResultConfiguration : IEntityTypeConfiguration<ExamResult>
    {
        public void Configure(EntityTypeBuilder<ExamResult> builder)
        {
            builder.ToTable("ExamResults");
            builder.HasKey(x => x.Id);

            builder.HasOne<Exam>().WithMany(x => x.ExamResults).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.ExamId, x.StudentId });
            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
