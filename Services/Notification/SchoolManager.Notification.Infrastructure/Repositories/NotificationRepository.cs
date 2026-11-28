using Microsoft.EntityFrameworkCore;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class NotificationRepository : BaseRepository<Domain.Notification>, INotificationRepository
    {
        public NotificationRepository(NotificationDbContext context) : base(context)
        {
        }

        public override Task<Domain.Notification?> GetByIdAsync(Guid id)
            => _dbSet.Include(x => x.Targets).FirstOrDefaultAsync(x => x.Id == id);

        public override async Task<List<Domain.Notification>> GetAllAsync()
            => await _dbSet.Include(x => x.Targets).ToListAsync();

        public async Task<List<Domain.Notification>> GetDueForPushAsync(DateOnly today, CancellationToken cancellationToken = default)
            => await _dbSet.Include(x => x.Targets)
                .Where(x => x.PushSentAt == null && x.SentOn <= today)
                .OrderBy(x => x.SentOn)
                .Take(100)
                .ToListAsync(cancellationToken);

        public async Task<List<Domain.Notification>> GetForStudentAsync(Guid? academicYearId, Guid gradeId, Guid sectionId, Guid studentId, DateOnly today, CancellationToken cancellationToken = default)
            => await _dbSet.AsNoTracking().Include(x => x.Targets)
                .Where(x => academicYearId == null || x.AcademicYearId == academicYearId)
                .Where(x => x.SentOn <= today)
                .Where(x => !x.Targets.Any()
                    // Class rows also carry their grade: match the class itself
                    || x.Targets.Any(t => t.SectionId == sectionId)
                    || x.Targets.Any(t => t.SectionId == null && t.GradeId == gradeId)
                    || x.Targets.Any(t => t.StudentId == studentId))
                .ToListAsync(cancellationToken);

        public async Task<List<Domain.Notification>> GetForTeacherAsync(Guid? academicYearId, Guid teacherId, DateOnly today, CancellationToken cancellationToken = default)
            => await _dbSet.AsNoTracking().Include(x => x.Targets)
                .Where(x => academicYearId == null || x.AcademicYearId == academicYearId)
                .Where(x => x.SentOn <= today)
                .Where(x => !x.Targets.Any() || x.Targets.Any(t => t.TeacherId == teacherId))
                .ToListAsync(cancellationToken);

        public async Task<List<Domain.Notification>> GetForDriverAsync(Guid? academicYearId, Guid busId, DateOnly today, CancellationToken cancellationToken = default)
            => await _dbSet.AsNoTracking().Include(x => x.Targets)
                .Where(x => academicYearId == null || x.AcademicYearId == academicYearId)
                .Where(x => x.SentOn <= today)
                .Where(x => !x.Targets.Any() || x.Targets.Any(t => t.BusId == busId))
                .ToListAsync(cancellationToken);

        private IQueryable<Domain.Notification> Sent(Guid? academicYearId, DateOnly today)
            => _dbSet.AsNoTracking()
                .Where(x => academicYearId == null || x.AcademicYearId == academicYearId)
                .Where(x => x.SentOn <= today);

        public Task<NotificationPage> PageForStudentAsync(Guid? academicYearId, Guid gradeId, Guid sectionId, Guid studentId, DateOnly today,
            NotificationTypeEnum? type, int skip, int take, CancellationToken cancellationToken = default)
            => PageAsync(Sent(academicYearId, today).Where(x => !x.Targets.Any()
                    || x.Targets.Any(t => t.SectionId == sectionId)
                    || x.Targets.Any(t => t.SectionId == null && t.GradeId == gradeId)
                    || x.Targets.Any(t => t.StudentId == studentId)),
                type, skip, take, cancellationToken);

        public Task<NotificationPage> PageForTeacherAsync(Guid? academicYearId, Guid teacherId, DateOnly today,
            NotificationTypeEnum? type, int skip, int take, CancellationToken cancellationToken = default)
            => PageAsync(Sent(academicYearId, today).Where(x => !x.Targets.Any() || x.Targets.Any(t => t.TeacherId == teacherId)),
                type, skip, take, cancellationToken);

        public Task<NotificationPage> PageForDriverAsync(Guid? academicYearId, Guid busId, DateOnly today,
            NotificationTypeEnum? type, int skip, int take, CancellationToken cancellationToken = default)
            => PageAsync(Sent(academicYearId, today).Where(x => !x.Targets.Any() || x.Targets.Any(t => t.BusId == busId)),
                type, skip, take, cancellationToken);

        /// <summary>Counts by type (all), the total of the type asked, and the page itself — newest first, by SQL</summary>
        private static async Task<NotificationPage> PageAsync(IQueryable<Domain.Notification> query, NotificationTypeEnum? type,
            int skip, int take, CancellationToken cancellationToken)
        {
            var counts = await query
                .GroupBy(x => x.NotificationType)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

            var ofType = type is NotificationTypeEnum t ? query.Where(x => x.NotificationType == t) : query;
            var total = type is NotificationTypeEnum k ? counts.GetValueOrDefault(k) : counts.Values.Sum();
            var items = await ofType
                .OrderByDescending(x => x.SentOn).ThenByDescending(x => x.AuditInfo.CreatedOn)
                .Skip(skip).Take(take)
                .Include(x => x.Targets)
                .ToListAsync(cancellationToken);
            return new NotificationPage(items, total, counts);
        }

        public Task<bool> ExistsForSourceEventAsync(Guid eventId, CancellationToken cancellationToken = default)
            => _dbSet.AnyAsync(x => x.SourceEventId == eventId, cancellationToken);

        public async Task<List<Domain.Notification>> GetForYearAsync(Guid? academicYearId, CancellationToken cancellationToken = default)
            => await _dbSet.AsNoTracking().Include(x => x.Targets)
                // The admin page lists the school office's notifications, not the system ones (homework)
                .Where(x => x.Source == NotificationSource.Admin)
                .Where(x => academicYearId == null || x.AcademicYearId == academicYearId)
                .ToListAsync(cancellationToken);

        public async Task<List<Domain.Notification>> GetLatestAdminAsync(int take, CancellationToken cancellationToken = default)
            => await _dbSet.AsNoTracking().Include(x => x.Targets)
                .Where(x => x.Source == NotificationSource.Admin)
                .OrderByDescending(x => x.SentOn).ThenByDescending(x => x.AuditInfo.CreatedOn)
                .Take(take)
                .ToListAsync(cancellationToken);
    }
}
