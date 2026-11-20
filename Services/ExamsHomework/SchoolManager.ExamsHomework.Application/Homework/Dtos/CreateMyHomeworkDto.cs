namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>New homework from the teacher app (the teacher, academic year and semester come from the server)</summary>
    public class CreateMyHomeworkDto
    {
        public Guid SubjectId { get; set; }
        public Guid GradeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly DueDate { get; set; }
        /// <summary>Academic year chosen in the app; null = the current one</summary>
        public Guid? AcademicYearId { get; set; }

        /// <summary>Semester chosen in the app; null = the running one</summary>
        public Guid? SemesterId { get; set; }

        public List<HomeworkQuestionItemDto>? Questions { get; set; }
    }
}
