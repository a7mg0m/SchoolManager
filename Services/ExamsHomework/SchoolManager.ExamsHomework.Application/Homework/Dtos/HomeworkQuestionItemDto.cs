namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>A question of a homework: its header and optional items (e.g. 1) … 2) …)</summary>
    public class HomeworkQuestionItemDto
    {
        public string Header { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
    }
}
