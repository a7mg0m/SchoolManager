using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class CreateExamCommandHandler : ICommandHandler<CreateExamCommand, Guid>
    {
        private readonly IExamRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIntegrationEventOutbox _outbox;

        private readonly ICurrentUserService _currentUser;

        public CreateExamCommandHandler(ICurrentUserService currentUser, IExamRepository repository, IUnitOfWork unitOfWork, IIntegrationEventOutbox outbox)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _outbox = outbox;
        }

        public async Task<Guid> Handle(CreateExamCommand command, CancellationToken cancellationToken)
        {
            // Second final: the original Final's subject / grade / class / year / semester
            if (command.ExamType == ExamType.SecondFinal)
            {
                var original = await SecondFinal.OriginalAsync(_repository, command.OriginalExamId, null, cancellationToken);
                command = command with
                {
                    SubjectId = original.SubjectId,
                    StageId = original.StageId,
                    GradeId = original.GradeId,
                    SectionId = original.SectionId,
                    AcademicYearId = original.AcademicYearId,
                    SemesterId = original.SemesterId,
                    OriginalExamId = original.Id
                };
            }

            var entity = new Exam(
                command.SubjectId,
                command.StageId,
                command.GradeId,
                command.SectionId,
                command.AcademicYearId,
                command.SemesterId,
                command.ExamType,
                command.Month,
                command.ExamDate,
                command.StartTime,
                command.EndTime,
                command.Duration,
                command.Weight,
                command.SuccessScore,
                command.AnswerModelUrl,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            if (command.TeacherId is Guid teacherId)
                entity.AssignTeacher(teacherId);
            entity.LinkOriginal(command.OriginalExamId);

            await _repository.AddAsync(entity);

            // A second final concerns only the students who failed: no notification to the whole class
            if (entity.ExamType == ExamType.SecondFinal)
            {
                await _unitOfWork.SaveChangesAsync();
                return entity.Id;
            }

            // The parents of the exam's class (or grade) get a notification (Notification service, through Kafka)
            _outbox.Enqueue(Topics.ExamsExamCreated, entity.Id.ToString(), new ExamCreatedIntegrationEvent
            {
                ExamId = entity.Id,
                ExamType = (int)entity.ExamType,
                SubjectId = entity.SubjectId,
                GradeId = entity.GradeId,
                SectionId = entity.SectionId,
                AcademicYearId = entity.AcademicYearId,
                SemesterId = entity.SemesterId,
                ExamDate = entity.ExamDate,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                Weight = entity.Weight,
                SuccessScore = entity.SuccessScore,
                CreatedBy = _currentUser.UserId
            });

            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
