using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAllStagesQuery() : ICommand<List<StageDto>>;
}
