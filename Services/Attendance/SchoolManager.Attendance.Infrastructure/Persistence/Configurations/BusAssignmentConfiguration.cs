using BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class BusAssignmentConfiguration : IEntityTypeConfiguration<BusAssignment>
    {
        public void Configure(EntityTypeBuilder<BusAssignment> builder)
        {
            builder.ToTable("BusAssignments");
            builder.HasKey(x => x.Id);

            // Ids of other services: plain columns, no foreign key
            builder.Property(x => x.StudentId).IsRequired();
            builder.Property(x => x.AcademicYearId).IsRequired();
            builder.Property(x => x.StudentFeeId);

            builder.Property(x => x.StartDate).IsRequired();

            ConfigureMoney(builder, x => x.Fee, "FeeAmount", "FeeCurrency");

            builder.HasOne<Bus>().WithMany().HasForeignKey(x => x.BusId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<BusRouteStop>().WithMany().HasForeignKey(x => x.BusRouteStopId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

            // Students of a bus; a student's assignments in a year
            builder.HasIndex(x => new { x.BusId, x.IsActive });
            builder.HasIndex(x => new { x.StudentId, x.AcademicYearId });
            builder.HasIndex(x => x.AcademicYearId);
            builder.HasIndex(x => x.StudentFeeId);

            builder.ConfigureAudit(x => x.AuditInfo);
        }

        private static void ConfigureMoney(EntityTypeBuilder<BusAssignment> builder,
            System.Linq.Expressions.Expression<Func<BusAssignment, Money?>> navigation, string amountColumn, string currencyColumn)
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
