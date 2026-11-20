using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class GetChildHomeworksQueryHandler(IHomeworkRepository homeworks) : IQueryHandler<GetChildHomeworksQuery, List<ChildHomeworkDto>>
    {
        public async Task<List<ChildHomeworkDto>> Handle(GetChildHomeworksQuery query, CancellationToken cancellationToken)
        {
            var child = query.Child;
            var published = await homeworks.GetPublishedForSectionAsync(child.AcademicYearId, child.SectionId, cancellationToken);

            return published
                // Only the semester chosen in the app (all semesters when none is chosen)
                .Where(x => query.SemesterId is not Guid semester || x.Homework.SemesterId == semester)
                .OrderByDescending(x => x.Homework.DueDate)
                .Select(x =>
                {
                    query.SubjectNames.TryGetValue(x.Homework.SubjectId, out var subject);
                    return new ChildHomeworkDto
                    {
                        Id = x.Homework.Id,
                        SubjectId = x.Homework.SubjectId,
                        SubjectNameAr = subject.Ar ?? string.Empty,
                        SubjectNameEn = subject.En ?? string.Empty,
                        Title = x.Homework.Title,
                        Description = x.Homework.Description,
                        DueDate = x.Homework.DueDate,
                        PublishedOn = x.PublishedOn,
                        Questions = HomeworkQuestionMapper.Map(x.Homework.HomeworkQuestions)
                    };
                })
                .ToList();
        }
    }
}
