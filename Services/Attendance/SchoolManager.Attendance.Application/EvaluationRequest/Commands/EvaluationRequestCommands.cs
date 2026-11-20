using BuildingBlocks.CQRS;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// The logged-in parent asks a teacher to evaluate their child (the child must be theirs). A student can
    /// have only one pending request to the same teacher.
    /// </summary>
    /// <param name="Authorization">The parent's "Authorization" header (to ask Management for the children)</param>
    public record CreateEvaluationRequestCommand(
        Guid StudentId,
        Guid TeacherId,
        Guid? SubjectId,
        string? Note,
        string Authorization
    ) : ICommand<Guid>;

    /// <summary>Only the parent who made the (pending) request can cancel it</summary>
    public record CancelEvaluationRequestCommand(Guid Id) : ICommand<bool>;
}
