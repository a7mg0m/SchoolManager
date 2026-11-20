using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class UpdateTeacherHomeworkCommandHandler(
        ICurrentUserService currentUser,
        IHomeworkRepository homeworks,
        IHomeworkQuestionRepository questions,
        IUnitOfWork unitOfWork) : ICommandHandler<UpdateTeacherHomeworkCommand, bool>
    {
        public async Task<bool> Handle(UpdateTeacherHomeworkCommand command, CancellationToken cancellationToken)
        {
            var homework = await homeworks.GetWithQuestionsAsync(command.Id, cancellationToken);
            if (homework is null || homework.TeacherId != command.TeacherId)
                return false;

            var now = DateTime.Now;
            homework.Update(
                command.Title.Trim(),
                command.Description.Trim(),
                command.DueDate,
                homework.AttachmentUrl,
                new AuditInfo(homework.AuditInfo.CreatedOn, homework.AuditInfo.CreatedBy, now, currentUser.UserId));

            // The questions are replaced by the edited list (order kept through CreatedOn)
            foreach (var old in homework.HomeworkQuestions.ToList())
            {
                homework.HomeworkQuestions.Remove(old);
                questions.Remove(old);
            }

            var order = 0;
            foreach (var q in command.Questions.Where(q => !string.IsNullOrWhiteSpace(q.Header)))
            {
                var items = q.Items.Select(i => i.Trim()).Where(i => i.Length > 0).ToList();
                var question = new HomeworkQuestion(homework.Id, q.Header.Trim(), items.Count > 0 ? items : null, null,
                    new AuditInfo(now.AddTicks(++order), currentUser.UserId));
                await questions.AddAsync(question);
            }

            await unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
