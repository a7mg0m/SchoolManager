using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public record CreateHomeworkPublishCommand(
        Guid HomeworkId,
        Guid GradeId,
        Guid SectionId,
        HomeworkStatusEnum Status,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
