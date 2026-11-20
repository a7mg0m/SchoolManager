using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>
    /// The teacher's subjects (linked to the teacher, or taught by them in a timetable), each with its grade
    /// and the grade's active classes, plus the current academic year and semester.
    /// </summary>
    public class GetMyClassesQueryHandler(
        ISubjectRepository subjects,
        ISectionRepository sections,
        IAcademicYearRepository academicYears,
        ISemesterRepository semesters) : IQueryHandler<GetMyClassesQuery, TeacherClassesDto>
    {
        public async Task<TeacherClassesDto> Handle(GetMyClassesQuery query, CancellationToken cancellationToken)
        {
            var teacherSubjects = await subjects.GetForTeacherAsync(query.TeacherId, cancellationToken);
            var gradeIds = teacherSubjects.Select(s => s.GradeId).Distinct().ToList();
            var gradeSections = gradeIds.Count == 0
                ? new List<Section>()
                : await sections.SearchAsync(s => gradeIds.Contains(s.GradeId) && s.IsActive);

            var today = DateOnly.FromDateTime(DateTime.Today);
            // The year chosen in the app, else the current one
            var chosen = query.AcademicYearId is Guid id ? await academicYears.GetByIdAsync(id) : null;
            var year = chosen ?? (await academicYears.SearchAsync(y => y.IsActive))
                .OrderByDescending(y => y.StartDate <= today && today <= y.EndDate)
                .ThenByDescending(y => y.StartDate)
                .FirstOrDefault();
            // The semester chosen in the app (only when it belongs to that year)
            var chosenSemester = year is not null && query.SemesterId is Guid semesterId
                ? (await semesters.SearchAsync(s => s.Id == semesterId && s.AcademicYearId == year.Id)).FirstOrDefault()
                : null;
            var semester = chosenSemester ?? (year is null
                ? null
                : (await semesters.SearchAsync(s => s.AcademicYearId == year.Id))
                    // The semester running today, else an active one, else the latest
                    .OrderByDescending(s => s.StartDate <= today && today <= s.EndDate)
                    .ThenByDescending(s => s.IsActive)
                    .ThenByDescending(s => s.StartDate)
                    .FirstOrDefault());

            return new TeacherClassesDto
            {
                AcademicYearId = year?.Id,
                SemesterId = semester?.Id,
                Subjects = teacherSubjects
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Grade?.Name.English).ThenBy(s => s.Name.English)
                    .Select(s => new TeacherSubjectDto
                    {
                        SubjectId = s.Id,
                        SubjectNameAr = s.Name.Arabic,
                        SubjectNameEn = s.Name.English,
                        GradeId = s.GradeId,
                        StageId = s.Grade?.StageId ?? Guid.Empty,
                        GradeNameAr = s.Grade?.Name.Arabic ?? string.Empty,
                        GradeNameEn = s.Grade?.Name.English ?? string.Empty,
                        Sections = gradeSections
                            .Where(sec => sec.GradeId == s.GradeId)
                            .OrderBy(sec => sec.Name.English)
                            .Select(sec => new TeacherSectionDto
                            {
                                SectionId = sec.Id,
                                NameAr = sec.Name.Arabic,
                                NameEn = sec.Name.English
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }
    }
}
