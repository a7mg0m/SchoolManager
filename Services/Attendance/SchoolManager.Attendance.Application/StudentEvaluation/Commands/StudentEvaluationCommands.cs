using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// The logged-in teacher evaluates a student in a lesson. One evaluation per student, slot and day:
    /// evaluating again updates it. It answers the given parent request, or else every pending request of the
    /// student to this teacher.
    /// </summary>
    public record EvaluateStudentCommand(
        Guid StudentId,
        Guid SubjectId,
        Guid TimetableSlotId,
        DateOnly Date,
        EvaluationRating Focus,
        EvaluationRating Interaction,
        EvaluationRating Behavior,
        HealthStatus HealthStatus,
        string? Comment,
        Guid? EvaluationRequestId,
        // An "all subjects" request is closed only when this is true (the teacher's last subject); a request for
        // one subject is closed by its evaluation
        bool CompleteRequest = true
    ) : ICommand<Guid>;

    /// <summary>Only the teacher who made the evaluation can change it</summary>
    public record UpdateStudentEvaluationCommand(
        Guid Id,
        EvaluationRating Focus,
        EvaluationRating Interaction,
        EvaluationRating Behavior,
        HealthStatus HealthStatus,
        string? Comment
    ) : ICommand<bool>;

    /// <summary>A teacher can delete only their own evaluation</summary>
    public record DeleteStudentEvaluationCommand(Guid Id) : ICommand<bool>;
}
