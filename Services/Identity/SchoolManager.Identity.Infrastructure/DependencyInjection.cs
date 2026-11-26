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

            // Parent sign-in: does the parent still have a current child? (Management service, through the gateway)
            services.AddHttpClient<IParentStudentsChecker, ManagementParentStudentsChecker>(client =>
            {
                client.BaseAddress = new Uri(configuration["ManagementService:BaseUrl"] ?? "http://localhost:53607/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            // Teacher sign-in: is the teacher still in service? (Management service, through the gateway)
            services.AddHttpClient<ITeacherServiceChecker, ManagementTeacherServiceChecker>(client =>
            {
                client.BaseAddress = new Uri(configuration["ManagementService:BaseUrl"] ?? "http://localhost:53607/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            // Kafka: teacher created / updated / deleted in Management -> teacher's User + UserProfile
            services.AddKafkaConsumer<TeacherIntegrationEvent, TeacherIntegrationEventHandler>(
                configuration, Topics.ManagementTeachers);

            // Kafka: parent created / updated / deleted in Management -> parent's User + UserProfile (ParentId)
            services.AddKafkaConsumer<ParentIntegrationEvent, ParentIntegrationEventHandler>(
                configuration, Topics.ManagementParents);

            // Kafka: bus created / updated / deleted in Attendance -> driver's User (role Driver) + UserProfile (BusId)
            services.AddKafkaConsumer<BusDriverIntegrationEvent, BusDriverIntegrationEventHandler>(
                configuration, Topics.AttendanceBusDrivers);

            return services;
        }
    }
}
