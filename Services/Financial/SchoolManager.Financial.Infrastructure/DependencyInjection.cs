using BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddFinancialInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<FinancialDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IExpenseCategoryRepository, ExpenseCategoryRepository>();
            services.AddScoped<IExpenseRepository, ExpenseRepository>();
            services.AddScoped<IFeesRepository, FeesRepository>();
            services.AddScoped<IStudentFeeRepository, StudentFeeRepository>();
            services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IRevenueCategoryRepository, RevenueCategoryRepository>();
            services.AddScoped<IRevenueRepository, RevenueRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }
    }
}
