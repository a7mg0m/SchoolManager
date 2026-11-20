namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>A homework of the logged-in teacher, with the classes it was published to (teacher app)</summary>
    public class TeacherHomeworkDto
    {
        public Guid Id { get; set; }
        public Guid SubjectId { get; set; }
        public string SubjectNameAr { get; set; } = string.Empty;
        public string SubjectNameEn { get; set; } = string.Empty;
        public Guid GradeId { get; set; }
        public string GradeNameAr { get; set; } = string.Empty;
        public string GradeNameEn { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly DueDate { get; set; }
        public DateTime CreatedOn { get; set; }
        public List<HomeworkQuestionItemDto> Questions { get; set; } = new();
        public List<PublishedSectionDto> PublishedSections { get; set; } = new();
    }
}
