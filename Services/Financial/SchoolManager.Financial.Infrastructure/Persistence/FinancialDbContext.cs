using Microsoft.EntityFrameworkCore;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class FinancialDbContext : DbContext
    {
        public FinancialDbContext(DbContextOptions<FinancialDbContext> options) : base(options)
        {
        }

        public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<Fee> Fees => Set<Fee>();
        public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<RevenueCategory> RevenueCategories => Set<RevenueCategory>();
        public DbSet<Revenue> Revenues => Set<Revenue>();
        public DbSet<StudentFee> StudentFees => Set<StudentFee>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinancialDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
