using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class PublishHomeworkCommandHandler(
        ICurrentUserService currentUser,
        IHomeworkPublishRepository publishes,
        IHomeworkRepository homeworks,
        IIntegrationEventOutbox outbox,
        IUnitOfWork unitOfWork) : ICommandHandler<PublishHomeworkCommand, int>
    {
        public async Task<int> Handle(PublishHomeworkCommand command, CancellationToken cancellationToken)
        {
            var already = (await publishes.GetForHomeworksAsync(new[] { command.HomeworkId }, cancellationToken))
                .Select(p => p.SectionId)
                .ToHashSet();

            var added = new List<Guid>();
            foreach (var sectionId in command.SectionIds.Distinct().Where(id => !already.Contains(id)))
            {
                await publishes.AddAsync(new HomeworkPublish(
                    command.HomeworkId, command.GradeId, sectionId, HomeworkStatusEnum.Pending, currentUser.UserId));
                added.Add(sectionId);
            }

            if (added.Count > 0)
            {
                // The parents of these classes get a notification (Notification service, through Kafka)
                var homework = await homeworks.GetByIdAsync(command.HomeworkId);
                if (homework is not null)
                {
                    outbox.Enqueue(Topics.ExamsHomeworkPublished, homework.Id.ToString(), new HomeworkPublishedIntegrationEvent
                    {
                        HomeworkId = homework.Id,
                        Title = homework.Title,
                        Description = homework.Description,
                        DueDate = homework.DueDate,
                        SubjectId = homework.SubjectId,
                        TeacherId = homework.TeacherId,
                        GradeId = command.GradeId,
                        AcademicYearId = homework.AcademicYearId,
                        SemesterId = homework.SemesterId,
                        SectionIds = added,
                        PublishedBy = currentUser.UserId
                    });
                }
                await unitOfWork.SaveChangesAsync();
            }
            return added.Count;
        }
    }
}
