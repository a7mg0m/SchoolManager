using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolManager.Financial.Application;
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
            services.AddScoped<IStudentFeePaymentRepository, StudentFeePaymentRepository>();
            services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IRevenueCategoryRepository, RevenueCategoryRepository>();
            services.AddScoped<IRevenueRepository, RevenueRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Parent app: which students belong to the logged-in parent (Management service, through the gateway)
            services.AddHttpClient<IParentChildrenService, ManagementParentChildrenService>(client =>
            {
                client.BaseAddress = new Uri(configuration["ManagementService:BaseUrl"] ?? "http://localhost:53607/");
                client.Timeout = TimeSpan.FromSeconds(15);
            });

            // Kafka: a student's bus assigned / fee changed / ended in Attendance -> the student's bus fee (Transportation)
            services.AddKafkaConsumer<BusFeeIntegrationEvent, BusFeeIntegrationEventHandler>(
                configuration, Topics.AttendanceBusFees);

            // Kafka: events (fee payment recorded → Notification service) are stored in the outbox with the business
            // change, then published in the background
            services.AddKafkaPublisher(configuration);
            services.AddScoped<IIntegrationEventOutbox, IntegrationEventOutbox>();
            services.AddHostedService<OutboxPublisherService>();

            return services;
        }
    }
}
