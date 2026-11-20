using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>Homework questions in the order they were added</summary>
    public static class HomeworkQuestionMapper
    {
        public static List<HomeworkQuestionItemDto> Map(IEnumerable<HomeworkQuestion> questions)
            => questions
                .OrderBy(q => q.AuditInfo.CreatedOn)
                .Select(q => new HomeworkQuestionItemDto { Header = q.QuestionHeader, Items = q.Questions ?? new List<string>() })
                .ToList();
    }
}
