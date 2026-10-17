using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateStageCommand(
        string NameAr,
        string NameEn,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
