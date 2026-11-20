namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>Edited homework from the teacher app (subject / grade / year / semester do not change)</summary>
    public class UpdateMyHomeworkDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly DueDate { get; set; }

        /// <summary>The questions after the edit (they replace the saved ones); each has a header and its sub-questions</summary>
        public List<HomeworkQuestionItemDto>? Questions { get; set; }
    }
}
