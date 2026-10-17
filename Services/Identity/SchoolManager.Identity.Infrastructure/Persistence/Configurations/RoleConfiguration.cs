using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Infrastructure
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public static readonly Guid SystemAdminId = new("00000000-0000-0000-0000-000000000001");
        public static readonly Guid AdminId = new("00000000-0000-0000-0000-000000000002");
        public static readonly Guid TeacherId = new("00000000-0000-0000-0000-000000000003");
        public static readonly Guid StudentId = new("00000000-0000-0000-0000-000000000004");
        public static readonly Guid ParentId = new("00000000-0000-0000-0000-000000000005");

        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Description).HasMaxLength(500);

            builder.OwnsOne(r => r.Name, name =>
            {
                name.Property(n => n.Arabic).HasColumnName("NameArabic").IsRequired().HasMaxLength(100);
                name.Property(n => n.English).HasColumnName("NameEnglish").IsRequired().HasMaxLength(100);

                name.HasData(
                    new { RoleId = SystemAdminId, Arabic = "مدير النظام", English = "SystemAdmin" },
                    new { RoleId = AdminId, Arabic = "مدير", English = "Admin" },
                    new { RoleId = TeacherId, Arabic = "معلم", English = "Teacher" },
                    new { RoleId = StudentId, Arabic = "طالب", English = "Student" },
                    new { RoleId = ParentId, Arabic = "ولي أمر", English = "Parent" });
            });

            builder.HasData(
                new { Id = SystemAdminId, Description = "System administrator" },
                new { Id = AdminId, Description = "Administrator" },
                new { Id = TeacherId, Description = "Teacher" },
                new { Id = StudentId, Description = "Student" },
                new { Id = ParentId, Description = "Parent" });
        }
    }
}
