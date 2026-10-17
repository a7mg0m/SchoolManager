using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateGradeCommand(
        Guid Id,
        string NameAr,
        string NameEn,
        Guid StageId,
        Guid UpdatedBy
    ) : ICommand<bool>;
}
