using BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class StudentFeeConfiguration : IEntityTypeConfiguration<StudentFee>
    {
        public void Configure(EntityTypeBuilder<StudentFee> builder)
        {
            builder.ToTable("StudentFees");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.StudentId).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.DueDate).IsRequired();

            builder.Ignore(x => x.NetAmount);
            builder.Ignore(x => x.Balance);

            ConfigureMoney(builder, x => x.TotalAmount, "TotalAmount", "TotalCurrency");
            ConfigureMoney(builder, x => x.DiscountAmount, "DiscountAmount", "DiscountCurrency");
            ConfigureMoney(builder, x => x.PaidAmount, "PaidAmount", "PaidCurrency");
        }

        private static void ConfigureMoney(EntityTypeBuilder<StudentFee> builder,
            System.Linq.Expressions.Expression<Func<StudentFee, Money?>> navigation, string amountColumn, string currencyColumn)
        {
            builder.OwnsOne(navigation, money =>
            {
                money.Property(m => m.Amount).HasColumnName(amountColumn).HasColumnType("decimal(18,2)").IsRequired();
                money.Property(m => m.Currency).HasColumnName(currencyColumn).HasMaxLength(10).IsRequired();
            });
            builder.Navigation(navigation).IsRequired();
        }
    }
}
