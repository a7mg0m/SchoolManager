using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class GradeConfiguration : IEntityTypeConfiguration<Grade>
	{
		public void Configure(EntityTypeBuilder<Grade> builder)
		{
            builder.ToTable("Grades");
            builder.HasKey(x => x.Id);

            builder.ConfigureLocalized(x => x.Name, "Name");
            builder.Ignore(x => x.AcademicYear);
            builder.HasMany(x => x.Sections).WithOne(s => s.Grade).HasForeignKey(s => s.GradeId).OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAudit(x => x.AuditInfo);
		}
	}
}
