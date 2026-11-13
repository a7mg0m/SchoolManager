namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>
    /// Bulk save of the results of one exam (one row per student).
    /// A null Score removes that student's existing result.
    /// </summary>
    public class SaveExamResultsDto
    {
        public Guid ExamId { get; set; }
        public List<StudentScoreDto> Results { get; set; } = new();
    }

    public class StudentScoreDto
    {
        public Guid StudentId { get; set; }
        public double? Score { get; set; }
    }

    /// <summary>What a bulk save changed.</summary>
    public record SaveExamResultsSummary(int Created, int Updated, int Removed);
}
