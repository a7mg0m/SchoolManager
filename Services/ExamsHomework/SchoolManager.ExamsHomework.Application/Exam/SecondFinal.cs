using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>A broken exam rule; Code is the error key the API answers (400)</summary>
    public class ExamRuleException(string code) : Exception(code)
    {
        public string Code { get; } = code;
    }

    /// <summary>
    /// Second final (دور ثانٍ): a resit of one Final exam, for the students who failed it. It takes the original's
    /// subject / grade / class / year / semester; a Final has at most one second final.
    /// </summary>
    public static class SecondFinal
    {
        public const string OriginalRequired = "OriginalFinalRequired";
        public const string OriginalNotFinal = "OriginalMustBeFinal";
        public const string AlreadyExists = "SecondFinalExists";
        public const string NotEligible = "NotEligibleForSecondFinal";
        public const string HasSecondFinal = "HasSecondFinal";

        /// <summary>The original Final of a second final being saved (selfId: the second final itself on update)</summary>
        public static async Task<Exam> OriginalAsync(IExamRepository exams, Guid? originalId, Guid? selfId, CancellationToken cancellationToken)
        {
            if (originalId is not Guid id || id == Guid.Empty) throw new ExamRuleException(OriginalRequired);
            var original = await exams.GetWithResultsAsync(id, cancellationToken) ?? throw new ExamRuleException(OriginalRequired);
            if (original.ExamType != ExamType.Final) throw new ExamRuleException(OriginalNotFinal);

            var others = await exams.SearchAsync(e => e.OriginalExamId == id && (selfId == null || e.Id != selfId));
            if (others.Count > 0) throw new ExamRuleException(AlreadyExists);
            return original;
        }

        /// <summary>Students who failed the Final (their score is below its pass mark), with their score</summary>
        public static Dictionary<Guid, double> FailedStudents(Exam original) =>
            original.ExamResults
                .Where(r => !original.IsPassed(r.Score))
                .GroupBy(r => r.StudentId)
                .ToDictionary(g => g.Key, g => g.First().Score);
    }

    /// <summary>A student who may sit a second final: failed the original Final with this score</summary>
    public record SecondFinalStudentDto(Guid StudentId, double OriginalScore, double OriginalWeight, double OriginalSuccessScore);

    /// <summary>
    /// The students of a second final (those who failed its original Final). For a Final: the students who failed it
    /// (who would sit its second final). Null when the exam does not exist or is neither.
    /// </summary>
    public record GetSecondFinalStudentsQuery(Guid ExamId) : IQuery<List<SecondFinalStudentDto>?>;

    public class GetSecondFinalStudentsQueryHandler(IExamRepository exams) : IQueryHandler<GetSecondFinalStudentsQuery, List<SecondFinalStudentDto>?>
    {
        public async Task<List<SecondFinalStudentDto>?> Handle(GetSecondFinalStudentsQuery query, CancellationToken cancellationToken)
        {
            var exam = await exams.GetWithResultsAsync(query.ExamId, cancellationToken);
            if (exam is null) return null;

            var original = exam.ExamType switch
            {
                ExamType.Final => exam,
                ExamType.SecondFinal when exam.OriginalExamId is Guid id => await exams.GetWithResultsAsync(id, cancellationToken),
                _ => null
            };
            if (original is null) return null;

            return SecondFinal.FailedStudents(original)
                .Select(p => new SecondFinalStudentDto(p.Key, p.Value, original.Weight, original.SuccessScore))
                .ToList();
        }
    }
}
