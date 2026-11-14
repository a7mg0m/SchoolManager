using BuildingBlocks;
using BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddManagementInfrastructure(this IServiceCollection services, IConfiguration configuration)
		{
			services.AddDbContext<ManagementDbContext>(options =>
				options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IAcademicYearRepository, AcademicYearRepository>();
            services.AddScoped<IGradeRepository, GradeRepository>();
            services.AddScoped<IParentRepository, ParentRepository>();
            services.AddScoped<ISchoolInfoRepository, SchoolInfoRepository>();
            services.AddScoped<ISectionRepository, SectionRepository>();
            services.AddScoped<ISemesterRepository, SemesterRepository>();
            services.AddScoped<IStageRepository, StageRepository>();
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<ISubjectRepository, SubjectRepository>();
            services.AddScoped<ITeacherRepository, TeacherRepository>();
            services.AddScoped<ITimetableRepository, TimetableRepository>();
            services.AddScoped<ITimetableSlotRepository, TimetableSlotRepository>();
			services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Kafka: events are stored in the outbox with the business change, then published in the background
            services.AddKafkaPublisher(configuration);
            services.AddScoped<IIntegrationEventOutbox, IntegrationEventOutbox>();
            services.AddHostedService<OutboxPublisherService>();

			return services;
		}
	}
}
