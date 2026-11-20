using BuildingBlocks.CQRS;

namespace SchoolManager.Attendance.Application
{
    public record GetStudentEvaluationByIdQuery(Guid Id) : IQuery<StudentEvaluationDto?>;

    /// <summary>The evaluations of one lesson (timetable slot) on a date</summary>
    public record GetEvaluationsBySlotQuery(Guid TimetableSlotId, DateOnly Date) : IQuery<List<StudentEvaluationDto>>;

    public record GetStudentEvaluationsQuery(Guid StudentId, DateOnly From, DateOnly To) : IQuery<List<StudentEvaluationDto>>;

    /// <summary>
    /// Parent app: evaluations of one of the logged-in parent's children. Null when the Management service
    /// could not tell the parent's children.
    /// </summary>
    public record GetMyChildEvaluationsQuery(Guid StudentId, DateOnly From, DateOnly To, string Authorization) : IQuery<List<StudentEvaluationDto>?>;
}
