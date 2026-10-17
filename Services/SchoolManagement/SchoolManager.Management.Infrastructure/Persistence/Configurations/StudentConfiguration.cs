using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class StudentConfiguration : IEntityTypeConfiguration<Student>
	{
		public void Configure(EntityTypeBuilder<Student> builder)
		{
            builder.ToTable("Students");
            builder.HasKey(x => x.Id);

            builder.ConfigureLocalized(x => x.FullName, "FullName");
            builder.Property(x => x.NationalId).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Gender).IsRequired().HasMaxLength(20);
            builder.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Address).IsRequired().HasMaxLength(500);
            builder.Property(x => x.Email).HasMaxLength(256);
            builder.HasIndex(x => x.NationalId).IsUnique();
            builder.HasOne(x => x.Grade).WithMany().HasForeignKey(x => x.GradeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<AcademicYear>().WithMany().HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAudit(x => x.AuditInfo);
		}
	}
}
