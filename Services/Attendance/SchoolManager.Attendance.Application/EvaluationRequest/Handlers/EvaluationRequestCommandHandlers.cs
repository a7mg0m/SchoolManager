using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// The parent's request: the student must be one of the parent's children (asked to Management), and must not
    /// already have a pending request to the same teacher.
    /// </summary>
    public class CreateEvaluationRequestCommandHandler : ICommandHandler<CreateEvaluationRequestCommand, Guid>
    {
        private readonly IEvaluationRequestRepository _repository;
        private readonly IParentChildrenService _parentChildren;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateEvaluationRequestCommandHandler(
            ICurrentUserService currentUser,
            IEvaluationRequestRepository repository,
            IParentChildrenService parentChildren,
            IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _parentChildren = parentChildren;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateEvaluationRequestCommand command, CancellationToken cancellationToken)
        {
            var children = await ParentChildrenCheck.MyChildrenAsync(_currentUser, _parentChildren, command.Authorization, cancellationToken)
                ?? throw new DomainException("Your children could not be checked right now. Please try again later.");
            ParentChildrenCheck.EnsureMyChild(children, command.StudentId);
            var parentId = _currentUser.ParentId!.Value;

            var studentId = command.StudentId;
            var teacherId = command.TeacherId;
            var pending = await _repository.SearchAsync(r => r.StudentId == studentId && r.TeacherId == teacherId
                && r.Status == EvaluationRequestStatus.Pending);
            if (pending.Count > 0)
                throw new DomainException("There is already a pending evaluation request for this student to this teacher.");

            var entity = EvaluationRequest.Create(studentId, parentId, teacherId, command.SubjectId, DateTime.UtcNow, command.Note,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
    }

    public class CancelEvaluationRequestCommandHandler : ICommandHandler<CancelEvaluationRequestCommand, bool>
    {
        private readonly IEvaluationRequestRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CancelEvaluationRequestCommandHandler(ICurrentUserService currentUser, IEvaluationRequestRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(CancelEvaluationRequestCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            if (_currentUser.ParentId != entity.ParentId)
                throw new DomainException("Only the parent who made the request can cancel it.");

            entity.Cancel();

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
