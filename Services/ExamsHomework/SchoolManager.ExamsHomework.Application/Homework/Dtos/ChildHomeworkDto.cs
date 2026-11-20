namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>A homework published to the child's class (parent app)</summary>
    public class ChildHomeworkDto
    {
        public Guid Id { get; set; }
        public Guid SubjectId { get; set; }
        public string SubjectNameAr { get; set; } = string.Empty;
        public string SubjectNameEn { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly DueDate { get; set; }
        public DateTime PublishedOn { get; set; }
        public List<HomeworkQuestionItemDto> Questions { get; set; } = new();
    }
}
