using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
    {
        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            builder.ToTable("Expenses");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.Reference).HasMaxLength(200);
            builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.ExpenseDate).IsRequired();

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
