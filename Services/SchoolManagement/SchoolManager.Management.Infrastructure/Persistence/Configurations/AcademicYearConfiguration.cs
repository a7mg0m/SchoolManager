using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
	{
		public void Configure(EntityTypeBuilder<AcademicYear> builder)
		{
            builder.ToTable("AcademicYears");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.HasMany(x => x.Semesters).WithOne(s => s.AcademicYear).HasForeignKey(s => s.AcademicYearId).OnDelete(DeleteBehavior.Cascade);
            builder.ConfigureAudit(x => x.AuditInfo);
		}
	}
}
