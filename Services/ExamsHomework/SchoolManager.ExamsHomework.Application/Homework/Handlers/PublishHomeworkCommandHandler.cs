using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class PublishHomeworkCommandHandler(
        ICurrentUserService currentUser,
        IHomeworkPublishRepository publishes,
        IUnitOfWork unitOfWork) : ICommandHandler<PublishHomeworkCommand, int>
    {
        public async Task<int> Handle(PublishHomeworkCommand command, CancellationToken cancellationToken)
        {
            var already = (await publishes.GetForHomeworksAsync(new[] { command.HomeworkId }, cancellationToken))
                .Select(p => p.SectionId)
                .ToHashSet();

            var added = 0;
            foreach (var sectionId in command.SectionIds.Distinct().Where(id => !already.Contains(id)))
            {
                await publishes.AddAsync(new HomeworkPublish(
                    command.HomeworkId, command.GradeId, sectionId, HomeworkStatusEnum.Pending, currentUser.UserId));
                added++;
            }

            if (added > 0)
                await unitOfWork.SaveChangesAsync();
            return added;
        }
    }
}
