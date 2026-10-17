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

        public UpdateExamCommandHandler(IExamRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(UpdateExamCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"Exam {command.Id} not found");

            entity.Update(
                command.SubjectId,
                command.GradeId,
                command.SectionId,
                command.AcademicYearId,
                command.SemesterId,
                command.ExamType,
                command.ExamDate,
                command.StartTime,
                command.EndTime,
                command.Duration,
                command.AnswerModelUrl,
                new AuditInfo(entity.AuditInfo.CreatedOn, entity.AuditInfo.CreatedBy, DateTime.Now, command.UpdatedBy));

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
