using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class EvaluationRequestDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid ParentId { get; set; }
        public Guid TeacherId { get; set; }

        /// <summary>null = in general</summary>
        public Guid? SubjectId { get; set; }

        /// <summary>UTC</summary>
        public DateTime RequestedAt { get; set; }
        public string? Note { get; set; }
        public EvaluationRequestStatus Status { get; set; }

        /// <summary>The evaluation that answered the request</summary>
        public Guid? EvaluationId { get; set; }

        /// <summary>UTC</summary>
        public DateTime? EvaluatedAt { get; set; }
    }
}
