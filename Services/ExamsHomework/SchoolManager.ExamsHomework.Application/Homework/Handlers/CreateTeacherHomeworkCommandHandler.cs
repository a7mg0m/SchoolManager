using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class CreateTeacherHomeworkCommandHandler(
        ICurrentUserService currentUser,
        IHomeworkRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<CreateTeacherHomeworkCommand, Guid>
    {
        public async Task<Guid> Handle(CreateTeacherHomeworkCommand command, CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            var homework = new Homework(
                command.SubjectId,
                command.TeacherId,
                command.GradeId,
                command.AcademicYearId,
                command.SemesterId,
                command.Title.Trim(),
                command.Description.Trim(),
                command.DueDate,
                null,
                new AuditInfo(now, currentUser.UserId));

            // Questions keep their order through CreatedOn (one tick apart)
            var order = 0;
            foreach (var q in command.Questions.Where(q => !string.IsNullOrWhiteSpace(q.Header)))
            {
                var items = q.Items.Select(i => i.Trim()).Where(i => i.Length > 0).ToList();
                homework.AddQuestion(homework.Id, q.Header.Trim(), items.Count > 0 ? items : null, null,
                    new AuditInfo(now.AddTicks(++order), currentUser.UserId));
            }

            await repository.AddAsync(homework);
            await unitOfWork.SaveChangesAsync();
            return homework.Id;
        }
    }
}
