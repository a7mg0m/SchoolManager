using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetGradesByStageIdQuery(Guid StageId) : ICommand<List<GradeDto>>;
}
