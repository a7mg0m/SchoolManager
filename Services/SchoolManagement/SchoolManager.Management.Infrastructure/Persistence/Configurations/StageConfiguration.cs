using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class StageConfiguration : IEntityTypeConfiguration<Stage>
	{
		public void Configure(EntityTypeBuilder<Stage> builder)
		{
            builder.ToTable("Stages");
            builder.HasKey(x => x.Id);

            builder.ConfigureLocalized(x => x.Name, "Name");
            builder.HasMany(x => x.Grades).WithOne(g => g.Stage).HasForeignKey(g => g.StageId).OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAudit(x => x.AuditInfo);
		}
	}
}
