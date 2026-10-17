namespace SchoolManager.ExamsHomework.Application
{
    public class CreateExamResultDto
    {
        public Guid ExamId { get; set; }
        public Guid StudentId { get; set; }
        public int Score { get; set; }
    }
}
