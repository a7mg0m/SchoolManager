using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolManager.Identity.Application;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddOptions<JwtSettings>()
                .Bind(configuration.GetSection("Jwt"));

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IUserProfileRepository, UserProfileRepository>();
            services.AddScoped<IEmployeeGroupRepository, EmployeeGroupRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IMenuItemRepository, MenuItemRepository>();
            services.AddScoped<IGroupPermissionRepository, GroupPermissionRepository>();
            services.AddScoped<EmployeeAccountService>();
            services.AddScoped<MyProfileReader>();
            services.AddScoped<ProfileSyncService>();
            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            // Kafka: teacher created / updated / deleted in Management -> teacher's User + UserProfile
            services.AddKafkaConsumer<TeacherIntegrationEvent, TeacherIntegrationEventHandler>(
                configuration, Topics.ManagementTeachers);

            return services;
        }
    }
}
