using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class ExamConfiguration : IEntityTypeConfiguration<Exam>
    {
        public void Configure(EntityTypeBuilder<Exam> builder)
        {
            builder.ToTable("Exams");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ExamType).HasConversion<int>();
            builder.Property(x => x.AnswerModelUrl).HasMaxLength(500);
            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
