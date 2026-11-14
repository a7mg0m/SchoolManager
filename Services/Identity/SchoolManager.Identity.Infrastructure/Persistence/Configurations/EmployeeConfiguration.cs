using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Infrastructure
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("Employees");

            builder.HasKey(e => e.Id);

            builder.OwnsOne(e => e.FullName, name =>
            {
                name.Property(n => n.Arabic).HasColumnName("FullNameAr").IsRequired().HasMaxLength(200);
                name.Property(n => n.English).HasColumnName("FullNameEn").IsRequired().HasMaxLength(200);
            });

            builder.Property(e => e.NationalId).IsRequired().HasMaxLength(50);
            builder.HasIndex(e => e.NationalId).IsUnique();

            builder.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(50);
            builder.Property(e => e.Email).HasMaxLength(256);
            builder.Property(e => e.Address).HasMaxLength(500);
            builder.Property(e => e.JobTitle).IsRequired().HasMaxLength(150);

            // Group relation is configured in EmployeeGroupConfiguration (Restrict delete)

            // Optional login account; one user can belong to at most one employee
            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(e => e.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");

            builder.OwnsOne(e => e.AuditInfo, audit =>
            {
                audit.Property(a => a.CreatedOn).HasColumnName("CreatedOn");
                audit.Property(a => a.CreatedBy).HasColumnName("CreatedBy");
                audit.Property(a => a.UpdatedOn).HasColumnName("UpdatedOn");
                audit.Property(a => a.UpdatedBy).HasColumnName("UpdatedBy");
            });
        }
    }
}
