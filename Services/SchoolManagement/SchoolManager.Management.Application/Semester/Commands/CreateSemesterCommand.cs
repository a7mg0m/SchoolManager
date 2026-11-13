using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateSemesterCommand(
        string Name,
        Guid AcademicYearId,
        DateOnly StartDate,
        DateOnly EndDate
    ) : ICommand<Guid>;
}
