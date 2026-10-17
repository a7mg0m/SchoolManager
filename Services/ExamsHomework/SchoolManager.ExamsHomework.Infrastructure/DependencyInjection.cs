using BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddExamsHomeworkInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ExamsHomeworkDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IExamRepository, ExamRepository>();
            services.AddScoped<IExamResultRepository, ExamResultRepository>();
            services.AddScoped<IHomeworkRepository, HomeworkRepository>();
            services.AddScoped<IHomeworkPublishRepository, HomeworkPublishRepository>();
            services.AddScoped<IHomeworkQuestionRepository, HomeworkQuestionRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
