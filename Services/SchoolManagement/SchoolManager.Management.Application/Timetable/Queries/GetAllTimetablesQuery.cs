using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    /// <summary>All timetables, or only those of an academic year / semester (filters of the admin list)</summary>
    public record GetAllTimetablesQuery(Guid? AcademicYearId = null, Guid? SemesterId = null) : ICommand<List<TimetableDto>>;
}
