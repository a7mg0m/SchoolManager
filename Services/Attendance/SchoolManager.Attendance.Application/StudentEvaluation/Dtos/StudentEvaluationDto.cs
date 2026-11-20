using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class StudentEvaluationDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid TeacherId { get; set; }
        public Guid SubjectId { get; set; }
        public Guid TimetableSlotId { get; set; }
        public DateOnly Date { get; set; }
        public EvaluationRating Focus { get; set; }
        public EvaluationRating Interaction { get; set; }
        public EvaluationRating Behavior { get; set; }
        public HealthStatus HealthStatus { get; set; }
        public string? Comment { get; set; }

        /// <summary>The parent's request it answered, if any</summary>
        public Guid? EvaluationRequestId { get; set; }
    }
}
