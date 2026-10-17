namespace SchoolManager.ExamsHomework.Application
{
    public class ExamResultDto
    {
        public Guid Id { get; set; }
        public Guid ExamId { get; set; }
        public Guid StudentId { get; set; }
        public double Score { get; set; }
    }
}
