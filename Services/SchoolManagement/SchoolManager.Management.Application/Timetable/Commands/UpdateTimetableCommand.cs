using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateTimetableCommand(
        Guid Id,
        Guid GradeId,
        Guid AcademicYearId,
        Guid UpdatedBy
    ) : ICommand<bool>;
}
