using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
    {
        public void Configure(EntityTypeBuilder<PaymentMethod> builder)
        {
            builder.ToTable("PaymentMethods");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.IsActive).IsRequired();
            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
