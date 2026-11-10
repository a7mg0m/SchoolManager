using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetActiveGradesQuery() : ICommand<List<GradeDto>>;
}
