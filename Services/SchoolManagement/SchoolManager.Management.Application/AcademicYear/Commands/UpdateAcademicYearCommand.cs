using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateAcademicYearCommand(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, bool IsActive, List<AcademicYearSemesterDto> Semesters) : ICommand<bool>;
}
