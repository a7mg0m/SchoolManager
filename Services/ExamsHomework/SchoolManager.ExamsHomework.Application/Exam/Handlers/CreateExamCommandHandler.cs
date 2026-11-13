using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class CreateExamCommandHandler : ICommandHandler<CreateExamCommand, Guid>
    {
        private readonly IExamRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public CreateExamCommandHandler(ICurrentUserService currentUser, IExamRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateExamCommand command, CancellationToken cancellationToken)
        {
            var entity = new Exam(
                command.SubjectId,
                command.StageId,
                command.GradeId,
                command.SectionId,
                command.AcademicYearId,
                command.SemesterId,
                command.ExamType,
                command.ExamDate,
                command.StartTime,
                command.EndTime,
                command.Duration,
                command.Weight,
                command.SuccessScore,
                command.AnswerModelUrl,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
