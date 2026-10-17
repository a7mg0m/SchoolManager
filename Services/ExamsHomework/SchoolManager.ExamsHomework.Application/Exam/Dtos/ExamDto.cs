using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class ExamDto
    {
        public Guid Id { get; set; }
        public Guid SubjectId { get; set; }
        public Guid GradeId { get; set; }
        public Guid SectionId { get; set; }
        public Guid AcademicYearId { get; set; }
        public Guid SemesterId { get; set; }
        public ExamType ExamType { get; set; }
        public DateOnly ExamDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public double Duration { get; set; }
        public string? AnswerModelUrl { get; set; }
    }
}
