using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>A second final of a Final exam, with its scores by student</summary>
    public record PromotionSecondFinalDto(Guid ExamId, DateOnly ExamDate, double Weight, double SuccessScore, Dictionary<Guid, double> Scores);

    /// <summary>
    /// A Final exam of the grade (for the whole grade when SectionId is null), with its scores by student and its
    /// second final (if any). A student passes a Final with its own score, or else with the second final's.
    /// </summary>
    public record PromotionFinalDto(
        Guid ExamId,
        Guid SubjectId,
        Guid? SectionId,
        Guid SemesterId,
        DateOnly ExamDate,
        double Weight,
        double SuccessScore,
        Dictionary<Guid, double> Scores,
        PromotionSecondFinalDto? SecondFinal);

    /// <summary>The Final exams (with their second finals) of a grade in an academic year: what promotion is decided on</summary>
    public record GetPromotionFinalsQuery(Guid AcademicYearId, Guid GradeId) : IQuery<List<PromotionFinalDto>>;

    public class GetPromotionFinalsQueryHandler(IExamRepository exams) : IQueryHandler<GetPromotionFinalsQuery, List<PromotionFinalDto>>
    {
        public async Task<List<PromotionFinalDto>> Handle(GetPromotionFinalsQuery query, CancellationToken cancellationToken)
        {
            var all = await exams.GetFinalsWithResultsAsync(query.AcademicYearId, query.GradeId, cancellationToken);
            var seconds = all
                .Where(e => e.ExamType == ExamType.SecondFinal && e.OriginalExamId != null)
                .GroupBy(e => e.OriginalExamId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            return all
                .Where(e => e.ExamType == ExamType.Final)
                .OrderBy(e => e.ExamDate)
                .Select(e =>
                {
                    seconds.TryGetValue(e.Id, out var second);
                    return new PromotionFinalDto(
                        e.Id, e.SubjectId, e.SectionId, e.SemesterId, e.ExamDate, e.Weight, e.SuccessScore, ScoresOf(e),
                        second is null ? null : new PromotionSecondFinalDto(second.Id, second.ExamDate, second.Weight, second.SuccessScore, ScoresOf(second)));
                })
                .ToList();
        }

        private static Dictionary<Guid, double> ScoresOf(Exam exam) =>
            exam.ExamResults.GroupBy(r => r.StudentId).ToDictionary(g => g.Key, g => g.First().Score);
    }
}
