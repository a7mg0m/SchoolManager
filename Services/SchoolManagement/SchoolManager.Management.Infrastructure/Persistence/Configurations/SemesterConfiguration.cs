using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class SemesterConfiguration : IEntityTypeConfiguration<Semester>
	{
		public void Configure(EntityTypeBuilder<Semester> builder)
		{
            builder.ToTable("Semesters");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.ConfigureAudit(x => x.AuditInfo);
		}
	}
}
