using Microsoft.EntityFrameworkCore;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class TimetableRepository : BaseRepository<Timetable>, ITimetableRepository
	{
		public TimetableRepository(ManagementDbContext context) : base(context)
		{
		}

		public override Task<Timetable?> GetByIdAsync(Guid id)
			=> _dbSet.Include(x => x.Slots).FirstOrDefaultAsync(x => x.Id == id);

		public override async Task<List<Timetable>> GetAllAsync()
			=> await _dbSet.Include(x => x.Slots).ToListAsync();

		public async Task<List<TimetableSlot>> GetTeacherSlotsAsync(Guid teacherId, Guid academicYearId, Guid? semesterId, CancellationToken cancellationToken = default)
		{
			// Timetables of the year that could apply: the semester's own, and the whole-year ones (no semester)
			var candidates = await _dbSet.AsNoTracking()
				.Where(t => t.AcademicYearId == academicYearId && (t.SemesterId == null || t.SemesterId == semesterId))
				.Select(t => new { t.Id, t.GradeId, t.SemesterId })
				.ToListAsync(cancellationToken);

			// Per grade: the semester's timetables when it has any, else the whole-year ones
			var timetableIds = candidates
				.GroupBy(t => t.GradeId)
				.SelectMany(g => semesterId is not null && g.Any(t => t.SemesterId == semesterId)
					? g.Where(t => t.SemesterId == semesterId)
					: g.Where(t => t.SemesterId == null))
				.Select(t => t.Id)
				.ToList();
			if (timetableIds.Count == 0)
				return new List<TimetableSlot>();

			return await _dbSet.AsNoTracking()
				.Where(t => timetableIds.Contains(t.Id))
				.SelectMany(t => t.Slots)
				.Where(s => s.TeacherId == teacherId)
				.Include(s => s.Subject)
				.Include(s => s.Grade)
				.Include(s => s.Section)
				.ToListAsync(cancellationToken);
		}

		public async Task<List<TimetableSlot>> GetSectionSlotsAsync(Guid academicYearId, Guid? semesterId, Guid gradeId, Guid sectionId, CancellationToken cancellationToken = default)
		{
			// No semester chosen: the one running today (if any)
			if (semesterId is null)
			{
				var today = DateOnly.FromDateTime(DateTime.Today);
				semesterId = await _context.Semesters.AsNoTracking()
					.Where(s => s.AcademicYearId == academicYearId && s.StartDate <= today && today <= s.EndDate)
					.Select(s => (Guid?)s.Id)
					.FirstOrDefaultAsync(cancellationToken);
			}

			var timetables = _dbSet.AsNoTracking().Where(t => t.AcademicYearId == academicYearId && t.GradeId == gradeId);

			// The semester's own timetables, else the ones saved without a semester (whole year)
			var ofSemester = semesterId is Guid id && await timetables.AnyAsync(t => t.SemesterId == id, cancellationToken)
				? timetables.Where(t => t.SemesterId == id)
				: timetables.Where(t => t.SemesterId == null);

			return await ofSemester
				.SelectMany(t => t.Slots)
				.Where(s => s.SectionId == sectionId)
				.Include(s => s.Subject)
				.Include(s => s.Teacher)
				.ToListAsync(cancellationToken);
		}
	}
}
