using BuildingBlocks;

namespace SchoolManager.Attendance.Domain;

/// <summary>
/// A parent asks a teacher (from the parent app) to evaluate their child. It stays Pending until the teacher
/// evaluates the student (<see cref="MarkEvaluated"/>) or the parent cancels it.
/// </summary>
public sealed class EvaluationRequest : BaseEntity
{
    public const int MaxNoteLength = 500;

    // Owned by other services (no navigation)
    public Guid StudentId { get; private set; }
    public Guid ParentId { get; private set; }
    public Guid TeacherId { get; private set; }

    /// <summary>The subject asked about; null = in general</summary>
    public Guid? SubjectId { get; private set; }

    /// <summary>UTC</summary>
    public DateTime RequestedAt { get; private set; }
    public string? Note { get; private set; }
    public EvaluationRequestStatus Status { get; private set; }

    /// <summary>The <see cref="StudentEvaluation"/> that answered the request</summary>
    public Guid? EvaluationId { get; private set; }

    /// <summary>UTC</summary>
    public DateTime? EvaluatedAt { get; private set; }
    public AuditInfo AuditInfo { get; private set; } = default!;

    private EvaluationRequest() { }

    public static EvaluationRequest Create(
        Guid studentId, Guid parentId, Guid teacherId, Guid? subjectId, DateTime requestedAt, string? note, AuditInfo auditInfo)
    {
        if (studentId == Guid.Empty)
            throw new DomainException("Student is required.");
        if (parentId == Guid.Empty)
            throw new DomainException("Parent is required.");
        if (teacherId == Guid.Empty)
            throw new DomainException("Teacher is required.");

        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (note?.Length > MaxNoteLength)
            throw new DomainException($"Note cannot exceed {MaxNoteLength} characters.");

        return new EvaluationRequest
        {
            StudentId = studentId,
            ParentId = parentId,
            TeacherId = teacherId,
            SubjectId = subjectId == Guid.Empty ? null : subjectId,
            RequestedAt = requestedAt,
            Note = note,
            Status = EvaluationRequestStatus.Pending,
            AuditInfo = auditInfo
        };
    }

    /// <summary>The teacher answered with an evaluation (only a pending request)</summary>
    public void MarkEvaluated(Guid evaluationId, DateTime at)
    {
        if (Status != EvaluationRequestStatus.Pending)
            throw new DomainException("Only a pending request can be evaluated.");
        if (evaluationId == Guid.Empty)
            throw new DomainException("Evaluation is required.");

        EvaluationId = evaluationId;
        EvaluatedAt = at;
        Status = EvaluationRequestStatus.Evaluated;
    }

    /// <summary>The parent no longer needs it (only a pending request)</summary>
    public void Cancel()
    {
        if (Status != EvaluationRequestStatus.Pending)
            throw new DomainException("Only a pending request can be cancelled.");

        Status = EvaluationRequestStatus.Cancelled;
    }
}
