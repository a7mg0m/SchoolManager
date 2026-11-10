using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateSectionCommand(
        string NameAr,
        string NameEn,
        Guid GradeId,
        Guid StageId,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
