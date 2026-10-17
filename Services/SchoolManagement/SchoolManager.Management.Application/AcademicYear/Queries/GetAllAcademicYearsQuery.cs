using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAllAcademicYearsQuery() : ICommand<List<AcademicYearDto>>;
}
