using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateSectionCommand(
        string NameAr,
        string NameEn,
        Guid GradeId,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
