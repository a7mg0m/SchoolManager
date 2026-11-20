using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class GetMyTimetableQueryHandler(
        ITimetableRepository timetables,
        IAcademicYearRepository academicYears,
        ISemesterRepository semesters) : IQueryHandler<GetMyTimetableQuery, TeacherTimetableDto>
    {
        public async Task<TeacherTimetableDto> Handle(GetMyTimetableQuery query, CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(query.Now);

            // The year chosen in the app, else the current one
            var chosen = query.AcademicYearId is Guid id ? await academicYears.GetByIdAsync(id) : null;
            var year = chosen ?? (await academicYears.SearchAsync(y => y.IsActive))
                .OrderByDescending(y => y.StartDate <= today && today <= y.EndDate)
                .ThenByDescending(y => y.StartDate)
                .FirstOrDefault();

            var result = new TeacherTimetableDto
            {
                Today = query.Now.DayOfWeek,
                Now = TimeOnly.FromDateTime(query.Now),
                AcademicYearId = year?.Id
            };
            if (year is null)
                return result;

            // The semester chosen in the app (when it belongs to the year), else the one running today
            var semester = query.SemesterId is Guid semesterId
                ? (await semesters.SearchAsync(s => s.Id == semesterId && s.AcademicYearId == year.Id)).FirstOrDefault()
                : null;
            semester ??= (await semesters.SearchAsync(s => s.AcademicYearId == year.Id))
                .FirstOrDefault(s => s.StartDate <= today && today <= s.EndDate);
            result.SemesterId = semester?.Id;

            var slots = await timetables.GetTeacherSlotsAsync(query.TeacherId, year.Id, semester?.Id, cancellationToken);

            result.Days = slots
                .GroupBy(s => s.Day)
                // Saturday first: fits both Saturday-Thursday and Sunday-Thursday school weeks
                .OrderBy(g => ((int)g.Key + 1) % 7)
                .Select(g => new TeacherTimetableDayDto
                {
                    DayOfWeek = g.Key,
                    Slots = g.OrderBy(s => s.StartTime).Select(ToDto).ToList()
                })
                .ToList();
            return result;
        }

        private static TeacherTimetableSlotDto ToDto(TimetableSlot slot) => new()
        {
            Id = slot.Id,
            NameAr = slot.Name.Arabic,
            NameEn = slot.Name.English,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            SubjectId = slot.SubjectId,
            SubjectNameAr = slot.Subject?.Name.Arabic ?? string.Empty,
            SubjectNameEn = slot.Subject?.Name.English ?? string.Empty,
            GradeId = slot.GradeId,
            GradeNameAr = slot.Grade?.Name.Arabic ?? string.Empty,
            GradeNameEn = slot.Grade?.Name.English ?? string.Empty,
            SectionId = slot.SectionId,
            SectionNameAr = slot.Section?.Name.Arabic ?? string.Empty,
            SectionNameEn = slot.Section?.Name.English ?? string.Empty
        };
    }
}
