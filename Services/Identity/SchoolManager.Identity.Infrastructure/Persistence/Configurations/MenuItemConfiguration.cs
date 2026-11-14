using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Infrastructure
{
    public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
    {
        public void Configure(EntityTypeBuilder<MenuItem> builder)
        {
            builder.ToTable("MenuItems");

            builder.HasKey(m => m.Id);

            builder.OwnsOne(m => m.Name, name =>
            {
                name.Property(n => n.Arabic).HasColumnName("NameAr").IsRequired().HasMaxLength(200);
                name.Property(n => n.English).HasColumnName("NameEn").IsRequired().HasMaxLength(200);

                // Seed: names of the initial menu (see MenuItemSeed)
                name.HasData(MenuItemSeed.Items
                    .Select(i => new { MenuItemId = i.Id, Arabic = i.NameAr, English = i.NameEn })
                    .ToArray());
            });

            builder.Property(m => m.Icon).HasMaxLength(100);
            builder.Property(m => m.Route).HasMaxLength(300);

            // Parent / children (a parent with children cannot be deleted)
            builder.HasOne(m => m.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(m => new { m.ParentId, m.Order });

            builder.OwnsOne(m => m.AuditInfo, audit =>
            {
                audit.Property(a => a.CreatedOn).HasColumnName("CreatedOn");
                audit.Property(a => a.CreatedBy).HasColumnName("CreatedBy");
                audit.Property(a => a.UpdatedOn).HasColumnName("UpdatedOn");
                audit.Property(a => a.UpdatedBy).HasColumnName("UpdatedBy");

                // Seed: audit of the initial menu (created by the system = empty user id)
                audit.HasData(MenuItemSeed.Items
                    .Select(i => new
                    {
                        MenuItemId = i.Id,
                        CreatedOn = MenuItemSeed.SeededOn,
                        CreatedBy = Guid.Empty,
                        UpdatedOn = (DateTime?)null,
                        UpdatedBy = (Guid?)null
                    })
                    .ToArray());
            });

            // Seed: initial menu = the links of the Angular sidebar
            builder.HasData(MenuItemSeed.Items
                .Select(i => new
                {
                    i.Id,
                    i.ParentId,
                    i.Icon,
                    i.Route,
                    i.Order,
                    IsActive = true
                })
                .ToArray());
        }
    }
}
