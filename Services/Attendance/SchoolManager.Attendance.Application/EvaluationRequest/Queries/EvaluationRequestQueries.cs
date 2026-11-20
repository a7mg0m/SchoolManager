using BuildingBlocks.CQRS;

namespace SchoolManager.Attendance.Application
{
    /// <summary>Teacher app: the pending requests to the logged-in teacher, oldest first</summary>
    public record GetMyPendingEvaluationRequestsQuery() : IQuery<List<EvaluationRequestDto>>;

    /// <summary>Parent app: the logged-in parent's requests (of one child when given), newest first, with their status</summary>
    public record GetMyEvaluationRequestsQuery(Guid? StudentId = null) : IQuery<List<EvaluationRequestDto>>;
}
