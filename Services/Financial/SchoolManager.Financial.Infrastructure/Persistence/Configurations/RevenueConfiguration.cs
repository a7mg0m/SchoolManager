using SchoolManager.Financial.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SchoolManager.Financial.Infrastructure
{
    public class RevenueConfiguration : IEntityTypeConfiguration<Revenue>
    {
        public void Configure(EntityTypeBuilder<Revenue> builder)
        {
            builder.ToTable("Revenues");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.Reference).HasMaxLength(200);
            builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.RevenueDate).IsRequired();

            builder.OwnsOne(x => x.Amount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(10).IsRequired();
            });
            builder.Navigation(x => x.Amount).IsRequired();

            builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
