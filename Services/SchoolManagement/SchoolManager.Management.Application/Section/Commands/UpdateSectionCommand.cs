using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateSectionCommand(
        Guid Id,
        string NameAr,
        string NameEn,
        Guid GradeId,
        Guid UpdatedBy
    ) : ICommand<bool>;
}
