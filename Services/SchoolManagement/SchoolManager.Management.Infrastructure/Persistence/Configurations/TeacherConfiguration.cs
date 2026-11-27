using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
	{
		public void Configure(EntityTypeBuilder<Teacher> builder)
		{
            builder.ToTable("Teachers");
            builder.HasKey(x => x.Id);

            builder.ConfigureLocalized(x => x.FullName, "FullName");
            builder.Property(x => x.NationalId).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Gender).IsRequired().HasMaxLength(20);
            builder.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Address).IsRequired().HasMaxLength(500);
            builder.Property(x => x.JobLevel).HasMaxLength(100);
            builder.Property(x => x.JobTitle).HasMaxLength(Teacher.MaxJobTitleLength);
            builder.Property(x => x.EmployeeNumber).HasMaxLength(Teacher.MaxEmployeeNumberLength);
            builder.Property(x => x.Qualification).HasMaxLength(Teacher.MaxQualificationLength);
            builder.Property(x => x.Specialization).HasMaxLength(Teacher.MaxSpecializationLength);
            builder.Property(x => x.Email).HasMaxLength(256);
            builder.HasIndex(x => x.NationalId).IsUnique();
            builder.Property(x => x.LoginName).HasMaxLength(100);
            builder.HasIndex(x => x.LoginName).IsUnique().HasFilter("[LoginName] IS NOT NULL");
            builder.ConfigureAudit(x => x.AuditInfo);
		}
	}
}
