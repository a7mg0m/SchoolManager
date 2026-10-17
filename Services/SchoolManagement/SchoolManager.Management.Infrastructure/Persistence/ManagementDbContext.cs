using Microsoft.EntityFrameworkCore;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class ManagementDbContext : DbContext
	{
		public ManagementDbContext(DbContextOptions<ManagementDbContext> options) : base(options)
		{
		}

        public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
        public DbSet<Grade> Grades => Set<Grade>();
        public DbSet<Parent> Parents => Set<Parent>();
        public DbSet<SchoolInfo> SchoolInfos => Set<SchoolInfo>();
        public DbSet<Section> Sections => Set<Section>();
        public DbSet<Semester> Semesters => Set<Semester>();
        public DbSet<Stage> Stages => Set<Stage>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<SubjectTeacher> SubjectTeachers => Set<SubjectTeacher>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Timetable> Timetables => Set<Timetable>();
        public DbSet<TimetableSlot> TimetableSlots => Set<TimetableSlot>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.ApplyConfigurationsFromAssembly(typeof(ManagementDbContext).Assembly);
			base.OnModelCreating(modelBuilder);
		}
	}
}
