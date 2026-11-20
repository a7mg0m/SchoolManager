using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// Creates or updates (same student, slot and day) the logged-in teacher's evaluation, then marks the parent's
    /// request(s) as evaluated: the given request, or every pending request of the student to this teacher.
    /// One SaveChanges.
    /// </summary>
    public class EvaluateStudentCommandHandler : ICommandHandler<EvaluateStudentCommand, Guid>
    {
        private readonly IStudentEvaluationRepository _repository;
        private readonly IEvaluationRequestRepository _requestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public EvaluateStudentCommandHandler(
            ICurrentUserService currentUser,
            IStudentEvaluationRepository repository,
            IEvaluationRequestRepository requestRepository,
            IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _requestRepository = requestRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(EvaluateStudentCommand command, CancellationToken cancellationToken)
        {
            var teacherId = _currentUser.TeacherId
                ?? throw new DomainException("Only a teacher can evaluate a student.");

            // The parent's request being answered (must be pending, for this student and this teacher)
            EvaluationRequest? request = null;
            if (command.EvaluationRequestId is Guid requestId && requestId != Guid.Empty)
            {
                request = await _requestRepository.GetByIdAsync(requestId)
                    ?? throw new DomainException("Evaluation request not found.");
                if (request.StudentId != command.StudentId || request.TeacherId != teacherId)
                    throw new DomainException("The evaluation request is not for this student and teacher.");
            }

            var studentId = command.StudentId;
            var slotId = command.TimetableSlotId;
            var date = command.Date;
            var evaluation = (await _repository.SearchAsync(e => e.StudentId == studentId && e.TimetableSlotId == slotId && e.Date == date))
                .FirstOrDefault();

            if (evaluation is null)
            {
                evaluation = StudentEvaluation.Create(studentId, teacherId, command.SubjectId, slotId, date,
                    command.Focus, command.Interaction, command.Behavior, command.HealthStatus, command.Comment,
                    new AuditInfo(DateTime.Now, _currentUser.UserId), request?.Id);
                await _repository.AddAsync(evaluation);
            }
            else
            {
                if (evaluation.TeacherId != teacherId)
                    throw new DomainException("Another teacher already evaluated this student in this lesson.");

                evaluation.Update(command.Focus, command.Interaction, command.Behavior, command.HealthStatus, command.Comment,
                    evaluation.AuditInfo.Updated(DateTime.Now, _currentUser.UserId));
                await _repository.UpdateAsync(evaluation);
            }

            var now = DateTime.UtcNow;
            if (request is not null)
            {
                request.MarkEvaluated(evaluation.Id, now);
                await _requestRepository.UpdateAsync(request);
            }
            else
            {
                var pending = await _requestRepository.SearchAsync(r => r.StudentId == studentId && r.TeacherId == teacherId
                    && r.Status == EvaluationRequestStatus.Pending);
                foreach (var pendingRequest in pending)
                {
                    pendingRequest.MarkEvaluated(evaluation.Id, now);
                    await _requestRepository.UpdateAsync(pendingRequest);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return evaluation.Id;
        }
    }

    public class UpdateStudentEvaluationCommandHandler : ICommandHandler<UpdateStudentEvaluationCommand, bool>
    {
        private readonly IStudentEvaluationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public UpdateStudentEvaluationCommandHandler(ICurrentUserService currentUser, IStudentEvaluationRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateStudentEvaluationCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            if (_currentUser.TeacherId != entity.TeacherId)
                throw new DomainException("Only the teacher who made the evaluation can change it.");

            entity.Update(command.Focus, command.Interaction, command.Behavior, command.HealthStatus, command.Comment,
                entity.AuditInfo.Updated(DateTime.Now, _currentUser.UserId));

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public class DeleteStudentEvaluationCommandHandler : ICommandHandler<DeleteStudentEvaluationCommand, bool>
    {
        private readonly IStudentEvaluationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public DeleteStudentEvaluationCommandHandler(ICurrentUserService currentUser, IStudentEvaluationRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteStudentEvaluationCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            // A teacher deletes only their own evaluations (the school office may delete any)
            if (_currentUser.TeacherId is Guid teacherId && teacherId != entity.TeacherId)
                throw new DomainException("Only the teacher who made the evaluation can delete it.");

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
