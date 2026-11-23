using BuildingBlocks;

namespace SchoolManager.Attendance.Domain;

/// <summary>
/// The teacher's evaluation of a student in one lesson (one per student, timetable slot and day):
/// focus, interaction and behavior (التركيز / التفاعل / الأخلاق), and how the student felt.
/// </summary>
public sealed class StudentEvaluation : BaseEntity
{
    public const int MaxCommentLength = 1000;

    // Owned by other services (no navigation)
    public Guid StudentId { get; private set; }
    public Guid TeacherId { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid TimetableSlotId { get; private set; }

    public DateOnly Date { get; private set; }
    public EvaluationRating Focus { get; private set; }
    public EvaluationRating Interaction { get; private set; }
    public EvaluationRating Behavior { get; private set; }
    public HealthStatus HealthStatus { get; private set; }
    public string? Comment { get; private set; }

    /// <summary>The parent's request this evaluation answers, if any</summary>
    public Guid? EvaluationRequestId { get; private set; }
    public AuditInfo AuditInfo { get; private set; } = default!;

    private StudentEvaluation() { }

    public static StudentEvaluation Create(
        Guid studentId, Guid teacherId, Guid subjectId, Guid timetableSlotId, DateOnly date,
        EvaluationRating focus, EvaluationRating interaction, EvaluationRating behavior,
        HealthStatus healthStatus, string? comment, AuditInfo auditInfo, Guid? evaluationRequestId = null)
    {
        if (studentId == Guid.Empty)
            throw new DomainException("Student is required.");
        if (teacherId == Guid.Empty)
            throw new DomainException("Teacher is required.");
        if (subjectId == Guid.Empty)
            throw new DomainException("Subject is required.");
        if (timetableSlotId == Guid.Empty)
            throw new DomainException("Timetable slot is required.");

        var evaluation = new StudentEvaluation
        {
            StudentId = studentId,
            TeacherId = teacherId,
            SubjectId = subjectId,
            TimetableSlotId = timetableSlotId,
            Date = date,
            EvaluationRequestId = evaluationRequestId == Guid.Empty ? null : evaluationRequestId
        };
        evaluation.Update(focus, interaction, behavior, healthStatus, comment, auditInfo);
        return evaluation;
    }

    public void Update(
        EvaluationRating focus, EvaluationRating interaction, EvaluationRating behavior,
        HealthStatus healthStatus, string? comment, AuditInfo auditInfo)
    {
        if (!Enum.IsDefined(focus) || !Enum.IsDefined(interaction) || !Enum.IsDefined(behavior))
            throw new DomainException("Ratings must be from Weak (1) to Excellent (5).");
        if (!Enum.IsDefined(healthStatus))
            throw new DomainException("Health status is not valid.");

        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (comment?.Length > MaxCommentLength)
            throw new DomainException($"Comment cannot exceed {MaxCommentLength} characters.");

        Focus = focus;
        Interaction = interaction;
        Behavior = behavior;
        HealthStatus = healthStatus;
        Comment = comment;
        AuditInfo = auditInfo;
    }

    /// <summary>The evaluation answers (part of) a parent's request, e.g. one subject of an "all subjects" request</summary>
    public void AnswerRequest(Guid evaluationRequestId)
    {
        if (evaluationRequestId == Guid.Empty)
            throw new DomainException("Evaluation request not found.");

        EvaluationRequestId = evaluationRequestId;
    }
}
