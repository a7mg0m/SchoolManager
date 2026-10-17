namespace SchoolManager.ExamsHomework.Application
{
    public class HomeworkQuestionDto
    {
        public Guid Id { get; set; }
        public Guid HomeworkId { get; set; }
        public string QuestionHeader { get; set; } = string.Empty;
        public List<string>? Questions { get; set; }
        public string? ImageUrl { get; set; }
    }
}
