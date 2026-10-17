namespace SchoolManager.ExamsHomework.Application
{
    public class CreateHomeworkQuestionDto
    {
        public Guid HomeworkId { get; set; }
        public string QuestionHeader { get; set; } = string.Empty;
        public List<string>? Questions { get; set; }
        public string? ImageUrl { get; set; }
    }
}
