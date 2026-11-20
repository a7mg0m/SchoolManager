using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>An exam the logged-in teacher added (teacher app list).</summary>
    public class TeacherExamDto
    {
        public Guid Id { get; set; }
        public Guid SubjectId { get; set; }
        public string SubjectNameAr { get; set; } = string.Empty;
        public string SubjectNameEn { get; set; } = string.Empty;
        public Guid GradeId { get; set; }
        public string GradeNameAr { get; set; } = string.Empty;
        public string GradeNameEn { get; set; } = string.Empty;
        public Guid? SectionId { get; set; }
        public string SectionNameAr { get; set; } = string.Empty;
        public string SectionNameEn { get; set; } = string.Empty;
        public Guid AcademicYearId { get; set; }
        public Guid SemesterId { get; set; }
        public ExamType ExamType { get; set; }
        /// <summary>Month of a Monthly exam (1..12); null for a quiz</summary>
        public int? Month { get; set; }
        public DateOnly ExamDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public double Duration { get; set; }
        /// <summary>Full mark</summary>
        public double Weight { get; set; }
        public double SuccessScore { get; set; }
        /// <summary>How many students have a score</summary>
        public int ResultCount { get; set; }
        /// <summary>Average of the given scores; null when none yet</summary>
        public double? AverageScore { get; set; }
    }

    /// <summary>
    /// New / edited exam from the teacher app. Only quizzes and monthly exams; a monthly exam needs its month.
    /// On edit the subject and grade stay as saved.
    /// </summary>
    public class SaveMyExamDto
    {
        public Guid SubjectId { get; set; }
        public Guid GradeId { get; set; }
        public Guid SectionId { get; set; }
        public ExamType ExamType { get; set; }
        public int? Month { get; set; }
        public DateOnly ExamDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public double Weight { get; set; }
        public double SuccessScore { get; set; }
        /// <summary>Academic year / semester chosen in the app (new exams); null = the current ones</summary>
        public Guid? AcademicYearId { get; set; }
        public Guid? SemesterId { get; set; }
    }

    /// <summary>Students of the exam's class with their scores (teacher app: scores screen).</summary>
    public class TeacherExamScoresDto
    {
        public Guid ExamId { get; set; }
        public double Weight { get; set; }
        public double SuccessScore { get; set; }
        public List<StudentExamScoreDto> Students { get; set; } = new();
    }

    public class StudentExamScoreDto
    {
        public Guid StudentId { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        /// <summary>null = no score yet</summary>
        public double? Score { get; set; }
    }

    /// <summary>Scores of the class from the teacher app; a null score removes the student's score.</summary>
    public class SaveMyExamScoresDto
    {
        public List<StudentScoreDto> Results { get; set; } = new();
    }
}
