using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateSemesterCommand(
        string Name,
        Guid AcademicYearId,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
