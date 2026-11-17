using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class GetChildExamsQueryHandler(IExamRepository repository) : IQueryHandler<GetChildExamsQuery, List<ChildExamDto>>
    {
        public async Task<List<ChildExamDto>> Handle(GetChildExamsQuery query, CancellationToken cancellationToken)
        {
            var child = query.Child;
            var exams = await repository.GetForStudentAsync(
                child.AcademicYearId, child.GradeId, child.SectionId, child.Id, cancellationToken);

            return exams
                .OrderBy(e => e.ExamDate).ThenBy(e => e.StartTime)
                .Select(e =>
                {
                    var result = e.ExamResults.FirstOrDefault(r => r.StudentId == child.Id);
                    query.SubjectNames.TryGetValue(e.SubjectId, out var subject);
                    return new ChildExamDto
                    {
                        Id = e.Id,
                        ExamType = e.ExamType,
                        Month = e.Month,
                        SubjectId = e.SubjectId,
                        SubjectNameAr = subject.Ar ?? string.Empty,
                        SubjectNameEn = subject.En ?? string.Empty,
                        ExamDate = e.ExamDate,
                        StartTime = e.StartTime,
                        EndTime = e.EndTime,
                        Duration = e.Duration,
                        Weight = e.Weight,
                        SuccessScore = e.SuccessScore,
                        AnswerModelUrl = e.AnswerModelUrl,
                        Score = result?.Score,
                        IsPassed = result is null ? null : e.IsPassed(result.Score),
                        Percentage = result is null || e.Weight <= 0
                            ? null
                            : Math.Round(Math.Clamp(result.Score / e.Weight * 100, 0, 100), 1)
                    };
                })
                .ToList();
        }
    }
}
