using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class HomeworkPublishConfiguration : IEntityTypeConfiguration<HomeworkPublish>
    {
        public void Configure(EntityTypeBuilder<HomeworkPublish> builder)
        {
            builder.ToTable("HomeworkPublishes");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).HasConversion<int>();

            builder.HasOne<Homework>().WithMany().HasForeignKey(x => x.HomeworkId).OnDelete(DeleteBehavior.Cascade);
            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
