using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateSubjectCommand(
        Guid Id,
        string NameAr,
        string NameEn,
        Guid StageId,
        Guid GradeId,
        List<Guid> TeacherIds
    ) : ICommand<bool>;
}
