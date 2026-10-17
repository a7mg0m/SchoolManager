using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record CreateHomeworkCommand(
        Guid SubjectId,
        Guid TeacherId,
        Guid GradeId,
        Guid AcademicYearId,
        Guid SemesterId,
        string Title,
        string Description,
        DateOnly DueDate,
        string? AttachmentUrl,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
