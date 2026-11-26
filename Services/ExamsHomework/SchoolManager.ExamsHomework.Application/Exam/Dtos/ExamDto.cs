using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class ExamDto
    {
        public Guid Id { get; set; }
        public Guid SubjectId { get; set; }
        public Guid GradeId { get; set; }
        public Guid StageId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid AcademicYearId { get; set; }
        public Guid SemesterId { get; set; }
        /// <summary>Teacher who added it from the teacher app; null for the school's exams</summary>
        public Guid? TeacherId { get; set; }
        public ExamType ExamType { get; set; }
        /// <summary>Second final: the Final exam it is the resit of</summary>
        public Guid? OriginalExamId { get; set; }
        /// <summary>Month of a Monthly exam (1 = January … 12 = December); null for the other types</summary>
        public int? Month { get; set; }
        public DateOnly ExamDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public double Duration { get; set; }
        public double Weight { get; set; }
        public double SuccessScore { get; set; }
        public string? AnswerModelUrl { get; set; }
    }
}
