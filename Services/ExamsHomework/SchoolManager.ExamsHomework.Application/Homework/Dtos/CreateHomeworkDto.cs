namespace SchoolManager.ExamsHomework.Application
{
    public class CreateHomeworkDto
    {
        public Guid SubjectId { get; set; }
        public Guid TeacherId { get; set; }
        public Guid GradeId { get; set; }
        public Guid AcademicYearId { get; set; }
        public Guid SemesterId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly DueDate { get; set; }
        public string? AttachmentUrl { get; set; }
    }
}
