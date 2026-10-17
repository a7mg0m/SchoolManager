using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateSubjectCommand(
        Guid Id,
        string NameAr,
        string NameEn,
        Guid GradeId,
        List<Guid> TeacherIds,
        Guid UpdatedBy
    ) : ICommand<bool>;
}
