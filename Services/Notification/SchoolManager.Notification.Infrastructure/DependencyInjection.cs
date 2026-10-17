using BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<NotificationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<INotificationTargetRepository, NotificationTargetRepository>();
            services.AddScoped<ISchoolEventRepository, SchoolEventRepository>();
            services.AddScoped<IEventTargetRepository, EventTargetRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
