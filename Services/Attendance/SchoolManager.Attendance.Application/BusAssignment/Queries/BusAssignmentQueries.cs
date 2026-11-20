using BuildingBlocks.CQRS;

namespace SchoolManager.Attendance.Application
{
    public record GetBusAssignmentByIdQuery(Guid Id) : IQuery<BusAssignmentDto?>;

    /// <param name="AcademicYearId">Only that year's assignments; null = all</param>
    public record GetAssignmentsByBusQuery(Guid BusId, Guid? AcademicYearId = null) : IQuery<List<BusAssignmentDto>>;

    public record GetAssignmentsByStudentQuery(Guid StudentId) : IQuery<List<BusAssignmentDto>>;
}
