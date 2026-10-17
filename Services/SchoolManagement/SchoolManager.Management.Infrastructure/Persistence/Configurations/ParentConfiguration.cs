using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class ParentConfiguration : IEntityTypeConfiguration<Parent>
	{
		public void Configure(EntityTypeBuilder<Parent> builder)
		{
            builder.ToTable("Parents");
            builder.HasKey(x => x.Id);

            builder.ConfigureLocalized(x => x.FullName, "FullName");
            builder.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Address).IsRequired().HasMaxLength(500);
            builder.Property(x => x.Email).HasMaxLength(256);
            builder.HasMany(x => x.Students).WithOne(s => s.Parent).HasForeignKey(s => s.ParentId).OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAudit(x => x.AuditInfo);
		}
	}
}
