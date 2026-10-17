using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateAcademicYearCommand(
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
