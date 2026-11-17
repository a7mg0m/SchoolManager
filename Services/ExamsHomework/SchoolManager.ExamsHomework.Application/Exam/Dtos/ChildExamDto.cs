using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>An exam of a parent's child, with the child's own result when it has been entered (parent app).</summary>
    public class ChildExamDto
    {
        public Guid Id { get; set; }
        /// <summary>Quiz = 1, Monthly = 2, MidTerm = 3, Final = 4</summary>
        public ExamType ExamType { get; set; }
        /// <summary>Month of a Monthly exam (1 = January … 12 = December); null for the other types</summary>
        public int? Month { get; set; }

        public Guid SubjectId { get; set; }
        public string SubjectNameAr { get; set; } = string.Empty;
        public string SubjectNameEn { get; set; } = string.Empty;

        public DateOnly ExamDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        /// <summary>Minutes</summary>
        public double Duration { get; set; }

        /// <summary>Full mark</summary>
        public double Weight { get; set; }
        /// <summary>Minimum score to pass</summary>
        public double SuccessScore { get; set; }
        public string? AnswerModelUrl { get; set; }

        /// <summary>The child's score; null until the result is entered.</summary>
        public double? Score { get; set; }
        public bool? IsPassed { get; set; }
        /// <summary>Score as a percentage of the full mark (0..100); null without a score.</summary>
        public double? Percentage { get; set; }
    }
}
