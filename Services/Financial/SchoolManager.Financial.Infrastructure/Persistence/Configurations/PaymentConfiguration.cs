using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.StudentFeeId).IsRequired();
            builder.Property(x => x.StudentId).IsRequired();
            builder.Property(x => x.ParentId).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.TransactionReference).HasMaxLength(200);
            builder.Property(x => x.FailureReason).HasMaxLength(1000);

            builder.OwnsOne(x => x.Amount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(10).IsRequired();
            });
            builder.Navigation(x => x.Amount).IsRequired();

            builder.HasOne(x => x.PaymentMethod).WithMany().HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
