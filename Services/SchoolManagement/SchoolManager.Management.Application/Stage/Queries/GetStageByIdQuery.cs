using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetStageByIdQuery(Guid Id) : ICommand<StageDto?>;
}
