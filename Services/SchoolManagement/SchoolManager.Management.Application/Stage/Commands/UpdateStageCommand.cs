using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateStageCommand(
        Guid Id,
        string NameAr,
        string NameEn,
        Guid UpdatedBy
    ) : ICommand<bool>;
}
