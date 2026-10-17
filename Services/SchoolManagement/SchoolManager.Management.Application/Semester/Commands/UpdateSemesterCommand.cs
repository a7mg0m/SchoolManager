using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateSemesterCommand(
        Guid Id,
        string Name,
        Guid AcademicYearId,
        Guid UpdatedBy
    ) : ICommand<bool>;
}
