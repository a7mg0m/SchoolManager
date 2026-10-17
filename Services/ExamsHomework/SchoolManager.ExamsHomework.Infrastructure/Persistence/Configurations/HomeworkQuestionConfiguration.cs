using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class HomeworkQuestionConfiguration : IEntityTypeConfiguration<HomeworkQuestion>
    {
        public void Configure(EntityTypeBuilder<HomeworkQuestion> builder)
        {
            builder.ToTable("HomeworkQuestions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.QuestionHeader).IsRequired().HasMaxLength(500);
            builder.Property(x => x.ImageUrl).HasMaxLength(500);
            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
