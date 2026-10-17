using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateGradeCommand(
        string NameAr,
        string NameEn,
        Guid StageId,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
