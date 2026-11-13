using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateAcademicYearCommand(
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        List<AcademicYearSemesterDto> Semesters
    ) : ICommand<Guid>;
}
