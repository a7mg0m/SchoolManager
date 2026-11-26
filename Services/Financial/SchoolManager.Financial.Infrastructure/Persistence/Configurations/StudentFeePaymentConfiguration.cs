using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class StudentFeePaymentConfiguration : IEntityTypeConfiguration<StudentFeePayment>
    {
        public void Configure(EntityTypeBuilder<StudentFeePayment> builder)
        {
            builder.ToTable("StudentFeePayments");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            builder.HasIndex(x => x.PaidOn);
            builder.HasIndex(x => x.StudentFeeId);

            // Deleting a student fee deletes its payment records
            builder.HasOne<StudentFee>().WithMany().HasForeignKey(x => x.StudentFeeId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
