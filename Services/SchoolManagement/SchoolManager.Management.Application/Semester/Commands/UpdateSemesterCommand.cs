using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateSemesterCommand(
        Guid Id,
        string Name,
        Guid AcademicYearId,
        DateOnly StartDate,
        DateOnly EndDate,
        Guid UpdatedBy
    ) : ICommand<bool>;
}
