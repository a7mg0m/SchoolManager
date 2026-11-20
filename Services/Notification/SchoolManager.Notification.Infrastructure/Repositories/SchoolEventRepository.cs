using Microsoft.EntityFrameworkCore;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class SchoolEventRepository : BaseRepository<SchoolEvent>, ISchoolEventRepository
    {
        public SchoolEventRepository(NotificationDbContext context) : base(context)
        {
        }

        public override Task<SchoolEvent?> GetByIdAsync(Guid id)
            => _dbSet.Include(x => x.Targets).FirstOrDefaultAsync(x => x.Id == id);

        public override async Task<List<SchoolEvent>> GetAllAsync()
            => await _dbSet.Include(x => x.Targets).ToListAsync();

        public async Task<List<SchoolEvent>> GetForStudentAsync(Guid? academicYearId, Guid? semesterId, Guid gradeId, Guid sectionId, Guid studentId, CancellationToken cancellationToken = default)
            => await _dbSet.Include(x => x.Targets)
                // The academic year / semester chosen in the app (all when none is given)
                .Where(e => academicYearId == null || e.AcademicYearId == academicYearId)
                .Where(e => semesterId == null || e.SemesterId == semesterId)
                .Where(e =>
                     (e.EventFor == EventForEnum.All
                        // Stage events are saved as one target per grade of the stage
                        || ((e.EventFor == EventForEnum.Stage || e.EventFor == EventForEnum.Grade)
                            && e.Targets.Any(t => t.GradeId == gradeId))
                        // Section targets also carry their grade: match the section itself
                        || (e.EventFor == EventForEnum.Section && e.Targets.Any(t => t.SectionId == sectionId))
                        || (e.EventFor == EventForEnum.Student && e.Targets.Any(t => t.StudentId == studentId))))
                .ToListAsync(cancellationToken);
    }
}
