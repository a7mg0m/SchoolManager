using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateTimetableCommand(
        Guid GradeId,
        Guid AcademicYearId,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
