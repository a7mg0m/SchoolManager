using SchoolManager.Financial.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SchoolManager.Financial.Infrastructure
{
    public class RevenueCategoryConfiguration : IEntityTypeConfiguration<RevenueCategory>
    {
        public void Configure(EntityTypeBuilder<RevenueCategory> builder)
        {
            builder.ToTable("RevenueCategories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
