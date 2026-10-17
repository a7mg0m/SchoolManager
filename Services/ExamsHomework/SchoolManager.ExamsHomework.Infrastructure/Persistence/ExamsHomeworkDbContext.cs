using Microsoft.EntityFrameworkCore;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class ExamsHomeworkDbContext : DbContext
    {
        public ExamsHomeworkDbContext(DbContextOptions<ExamsHomeworkDbContext> options) : base(options)
        {
        }

        public DbSet<Exam> Exams => Set<Exam>();
        public DbSet<ExamResult> ExamResults => Set<ExamResult>();
        public DbSet<Homework> Homeworks => Set<Homework>();
        public DbSet<HomeworkPublish> HomeworkPublishes => Set<HomeworkPublish>();
        public DbSet<HomeworkQuestion> HomeworkQuestions => Set<HomeworkQuestion>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExamsHomeworkDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
