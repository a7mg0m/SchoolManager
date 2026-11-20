using Microsoft.EntityFrameworkCore;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class AttendanceDbContext : DbContext
    {
        public AttendanceDbContext(DbContextOptions<AttendanceDbContext> options) : base(options)
        {
        }

        public DbSet<DailyAttendance> DailyAttendances => Set<DailyAttendance>();
        public DbSet<PeriodAttendance> PeriodAttendances => Set<PeriodAttendance>();
        public DbSet<StudentEvaluation> StudentEvaluations => Set<StudentEvaluation>();
        public DbSet<EvaluationRequest> EvaluationRequests => Set<EvaluationRequest>();
        public DbSet<BusRoute> BusRoutes => Set<BusRoute>();
        public DbSet<BusRouteStop> BusRouteStops => Set<BusRouteStop>();
        public DbSet<Bus> Buses => Set<Bus>();
        public DbSet<BusAssignment> BusAssignments => Set<BusAssignment>();
        public DbSet<Trip> Trips => Set<Trip>();
        public DbSet<TripStudent> TripStudents => Set<TripStudent>();

        // Transactional outbox (Kafka)
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AttendanceDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
