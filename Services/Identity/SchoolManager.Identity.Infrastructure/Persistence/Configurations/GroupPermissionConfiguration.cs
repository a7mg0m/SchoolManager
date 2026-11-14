using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Infrastructure
{
    public class GroupPermissionConfiguration : IEntityTypeConfiguration<GroupPermission>
    {
        public void Configure(EntityTypeBuilder<GroupPermission> builder)
        {
            builder.ToTable("GroupPermissions");

            builder.HasKey(p => p.Id);

            // Deleting a group or a menu item removes its permissions
            builder.HasOne(p => p.EmployeeGroup)
                .WithMany()
                .HasForeignKey(p => p.EmployeeGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.MenuItem)
                .WithMany()
                .HasForeignKey(p => p.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // One row per group and menu item
            builder.HasIndex(p => new { p.EmployeeGroupId, p.MenuItemId }).IsUnique();
            builder.HasIndex(p => p.MenuItemId);

            builder.OwnsOne(p => p.AuditInfo, audit =>
            {
                audit.Property(a => a.CreatedOn).HasColumnName("CreatedOn");
                audit.Property(a => a.CreatedBy).HasColumnName("CreatedBy");
                audit.Property(a => a.UpdatedOn).HasColumnName("UpdatedOn");
                audit.Property(a => a.UpdatedBy).HasColumnName("UpdatedBy");
            });
        }
    }
}
