using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class UpdateExamCommandHandler : ICommandHandler<UpdateExamCommand>
    {
        private readonly IExamRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public UpdateExamCommandHandler(ICurrentUserService currentUser, IExamRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(UpdateExamCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"Exam {command.Id} not found");

            // A Final that has a second final stays a Final
            if (entity.ExamType == ExamType.Final && command.ExamType != ExamType.Final
                && (await _repository.SearchAsync(e => e.OriginalExamId == entity.Id)).Count > 0)
                throw new ExamRuleException(SecondFinal.HasSecondFinal);

            // Second final: the original Final's subject / grade / class / year / semester
            if (command.ExamType == ExamType.SecondFinal)
            {
                var original = await SecondFinal.OriginalAsync(_repository, command.OriginalExamId ?? entity.OriginalExamId, entity.Id, cancellationToken);
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

            entity.Update(
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
                new AuditInfo(entity.AuditInfo.CreatedOn, entity.AuditInfo.CreatedBy, DateTime.Now, _currentUser.UserId));

            entity.LinkOriginal(command.OriginalExamId);

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
