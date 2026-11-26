using BuildingBlocks;
using BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolManager.Attendance.Application;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAttendanceInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AttendanceDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IDailyAttendanceRepository, DailyAttendanceRepository>();
            services.AddScoped<IStudentEvaluationRepository, StudentEvaluationRepository>();
            services.AddScoped<IEvaluationRequestRepository, EvaluationRequestRepository>();
            services.AddScoped<IBusRouteRepository, BusRouteRepository>();
            services.AddScoped<IBusRepository, BusRepository>();
            services.AddScoped<IBusAssignmentRepository, BusAssignmentRepository>();
            services.AddScoped<ITripRepository, TripRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Kafka: events (bus fees → Financial) are stored in the outbox with the business change, then published in the background
            services.AddKafkaPublisher(configuration);
            services.AddScoped<IIntegrationEventOutbox, IntegrationEventOutbox>();
            services.AddHostedService<OutboxPublisherService>();

            // Parent app: which students belong to the logged-in parent (Management service, through the gateway)
            services.AddHttpClient<IParentChildrenService, ManagementParentChildrenService>(client =>
            {
                client.BaseAddress = new Uri(configuration["ManagementService:BaseUrl"] ?? "http://localhost:53607/");
                client.Timeout = TimeSpan.FromSeconds(15);
            });

            // Teacher app: does the logged-in teacher teach a student (Management service, through the gateway)
            services.AddHttpClient<ITeacherStudentsService, ManagementTeacherStudentsService>(client =>
            {
                client.BaseAddress = new Uri(configuration["ManagementService:BaseUrl"] ?? "http://localhost:53607/");
                client.Timeout = TimeSpan.FromSeconds(15);
            });

            // Driver app: names, classes and parents' phones of the students on a trip (Management service, through the gateway)
            services.AddHttpClient<IBusRiderDirectory, ManagementBusRiderDirectory>(client =>
            {
                client.BaseAddress = new Uri(configuration["ManagementService:BaseUrl"] ?? "http://localhost:53607/");
                client.Timeout = TimeSpan.FromSeconds(15);
            });

            return services;
        }
    }
}
