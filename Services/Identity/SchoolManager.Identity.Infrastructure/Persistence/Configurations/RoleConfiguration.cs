using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Infrastructure
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public static readonly Guid SystemAdminId = RoleIds.SystemAdmin;
        public static readonly Guid AdminId = RoleIds.Admin;
        public static readonly Guid TeacherId = RoleIds.Teacher;
        public static readonly Guid StudentId = RoleIds.Student;
        public static readonly Guid ParentId = RoleIds.Parent;

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
