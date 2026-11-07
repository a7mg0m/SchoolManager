using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetActiveStagesQuery() : ICommand<List<StageDto>>;
}
