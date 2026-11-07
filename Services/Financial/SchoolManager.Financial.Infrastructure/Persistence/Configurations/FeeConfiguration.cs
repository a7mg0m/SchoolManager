using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class FeeConfiguration : IEntityTypeConfiguration<Fee>
    {
        public void Configure(EntityTypeBuilder<Fee> builder)
        {
            builder.ToTable("Fees");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.GradeId).IsRequired();
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.IsMandatory).IsRequired();

            builder.OwnsOne(x => x.Amount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(10).IsRequired();
            });
            builder.Navigation(x => x.Amount).IsRequired();

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
