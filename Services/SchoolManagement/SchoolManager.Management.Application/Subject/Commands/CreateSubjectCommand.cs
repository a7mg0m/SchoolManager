using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateSubjectCommand(
        string NameAr,
        string NameEn,
        Guid GradeId,
        List<Guid> TeacherIds,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
