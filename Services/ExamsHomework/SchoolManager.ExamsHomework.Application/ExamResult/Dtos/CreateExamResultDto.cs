namespace SchoolManager.ExamsHomework.Application
{
    public class CreateExamResultDto
    {
        public Guid ExamId { get; set; }
        public Guid StudentId { get; set; }
        public double Score { get; set; }
    }
}
