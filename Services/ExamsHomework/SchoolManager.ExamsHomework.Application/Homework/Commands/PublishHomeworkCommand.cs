using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>
    /// Publish a homework to classes of its grade; classes it is already published to are skipped.
    /// Returns the number of classes it was newly published to.
    /// </summary>
    public record PublishHomeworkCommand(Guid HomeworkId, Guid GradeId, IReadOnlyList<Guid> SectionIds) : ICommand<int>;
}
