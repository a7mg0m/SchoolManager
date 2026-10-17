using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class HomeworkConfiguration : IEntityTypeConfiguration<Homework>
    {
        public void Configure(EntityTypeBuilder<Homework> builder)
        {
            builder.ToTable("Homeworks");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).IsRequired().HasMaxLength(2000);
            builder.Property(x => x.AttachmentUrl).HasMaxLength(500);

            builder.HasMany(x => x.HomeworkQuestions)
                .WithOne()
                .HasForeignKey(q => q.HomeworkId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
