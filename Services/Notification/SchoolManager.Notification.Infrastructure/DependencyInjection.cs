using BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolManager.Notification.Application;
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
            services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
            services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
            services.AddScoped<IMessageStatusRepository, MessageStatusRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Push notifications (Firebase Cloud Messaging)
            services.AddScoped<IUserDeviceTokenRepository, UserDeviceTokenRepository>();
            services.AddSingleton<IPushNotificationSender, FcmPushNotificationSender>();
            services.AddScoped<INotificationRecipientResolver, NotificationRecipientResolver>();
            services.AddScoped<INotificationPushService, NotificationPushService>();
            services.AddSingleton<ServiceTokenProvider>();
            // Notifications created for a later day are pushed on that day
            services.AddHostedService<DuePushNotificationsService>();

            // Chat: names (Identity service, through the gateway, with the user's token), cached in memory.
            // A short timeout: the chat must not wait for Identity.
            services.AddMemoryCache();
            services.AddHttpContextAccessor();
            services.AddTransient<ForwardAuthorizationHandler>();
            services.AddHttpClient<IUserProfileService, IdentityUserProfileService>(client =>
            {
                client.BaseAddress = new Uri(configuration["IdentityService:BaseUrl"] ?? "http://localhost:53607/");
                client.Timeout = TimeSpan.FromSeconds(5);
            }).AddHttpMessageHandler<ForwardAuthorizationHandler>();

            // Parent app: the parent's children (Management service, through the gateway)
            services.AddHttpClient<IManagementClient, ManagementClient>(client =>
            {
                client.BaseAddress = new Uri(configuration["ManagementService:BaseUrl"] ?? "http://localhost:53607/");
                client.Timeout = TimeSpan.FromSeconds(15);
            });

            return services;
        }
    }
}
