using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAllGradesQuery() : ICommand<List<GradeDto>>;
}
