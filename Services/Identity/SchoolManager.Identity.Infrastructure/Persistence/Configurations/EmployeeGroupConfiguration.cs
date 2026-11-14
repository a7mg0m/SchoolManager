using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Infrastructure
{
    public class EmployeeGroupConfiguration : IEntityTypeConfiguration<EmployeeGroup>
    {
        public void Configure(EntityTypeBuilder<EmployeeGroup> builder)
        {
            builder.ToTable("EmployeeGroups");

            builder.HasKey(g => g.Id);

            builder.OwnsOne(g => g.Name, name =>
            {
                name.Property(n => n.Arabic).HasColumnName("NameAr").IsRequired().HasMaxLength(200);
                name.Property(n => n.English).HasColumnName("NameEn").IsRequired().HasMaxLength(200);
            });

            builder.Property(g => g.Description).HasMaxLength(500);

            builder.HasMany(g => g.Employees)
                .WithOne(e => e.EmployeeGroup)
                .HasForeignKey(e => e.EmployeeGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.OwnsOne(g => g.AuditInfo, audit =>
            {
                audit.Property(a => a.CreatedOn).HasColumnName("CreatedOn");
                audit.Property(a => a.CreatedBy).HasColumnName("CreatedBy");
                audit.Property(a => a.UpdatedOn).HasColumnName("UpdatedOn");
                audit.Property(a => a.UpdatedBy).HasColumnName("UpdatedBy");
            });
        }
    }
}
