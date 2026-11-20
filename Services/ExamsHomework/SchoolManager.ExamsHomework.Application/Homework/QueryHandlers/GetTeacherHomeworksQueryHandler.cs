using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class GetTeacherHomeworksQueryHandler(
        IHomeworkRepository homeworks,
        IHomeworkPublishRepository publishes) : IQueryHandler<GetTeacherHomeworksQuery, List<TeacherHomeworkDto>>
    {
        public async Task<List<TeacherHomeworkDto>> Handle(GetTeacherHomeworksQuery query, CancellationToken cancellationToken)
        {
            var list = (await homeworks.GetForTeacherAsync(query.TeacherId, cancellationToken))
                // Only the chosen academic year (all years when none is chosen)
                .Where(h => query.AcademicYearId is not Guid year || h.AcademicYearId == year)
                .Where(h => query.SemesterId is not Guid semester || h.SemesterId == semester)
                .ToList();
            var published = (await publishes.GetForHomeworksAsync(list.Select(h => h.Id).ToList(), cancellationToken))
                .ToLookup(p => p.HomeworkId);
            var sections = query.Classes.Subjects
                .SelectMany(s => s.Sections)
                .GroupBy(s => s.Id)
                .ToDictionary(g => g.Key, g => g.First());

            return list.Select(h =>
            {
                var subject = query.Classes.Subject(h.SubjectId, h.GradeId)
                    ?? query.Classes.Subjects.FirstOrDefault(s => s.SubjectId == h.SubjectId);
                return new TeacherHomeworkDto
                {
                    Id = h.Id,
                    SubjectId = h.SubjectId,
                    SubjectNameAr = subject?.SubjectNameAr ?? string.Empty,
                    SubjectNameEn = subject?.SubjectNameEn ?? string.Empty,
                    GradeId = h.GradeId,
                    GradeNameAr = subject?.GradeNameAr ?? string.Empty,
                    GradeNameEn = subject?.GradeNameEn ?? string.Empty,
                    Title = h.Title,
                    Description = h.Description,
                    DueDate = h.DueDate,
                    CreatedOn = h.AuditInfo.CreatedOn,
                    Questions = HomeworkQuestionMapper.Map(h.HomeworkQuestions),
                    PublishedSections = published[h.Id]
                        .OrderBy(p => p.PublishedOn)
                        .Select(p =>
                        {
                            sections.TryGetValue(p.SectionId, out var section);
                            return new PublishedSectionDto
                            {
                                SectionId = p.SectionId,
                                NameAr = section?.NameAr ?? string.Empty,
                                NameEn = section?.NameEn ?? string.Empty,
                                PublishedOn = p.PublishedOn
                            };
                        })
                        .ToList()
                };
            }).ToList();
        }
    }
}
