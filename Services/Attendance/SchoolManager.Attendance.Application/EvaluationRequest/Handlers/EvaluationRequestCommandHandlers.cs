using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
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
        private readonly IIntegrationEventOutbox _outbox;

        public CreateEvaluationRequestCommandHandler(
            ICurrentUserService currentUser,
            IEvaluationRequestRepository repository,
            IParentChildrenService parentChildren,
            IUnitOfWork unitOfWork,
            IIntegrationEventOutbox outbox)
        {
            _outbox = outbox;
            _currentUser = currentUser;
            _repository = repository;
            _parentChildren = parentChildren;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateEvaluationRequestCommand command, CancellationToken cancellationToken)
        {
            if (_currentUser.ParentId is null)
                throw new DomainException("Only a parent can do this.");
            var children = await _parentChildren.GetMyChildrenAsync(command.Authorization, cancellationToken)
                ?? throw new DomainException("Your children could not be checked right now. Please try again later.");
            ParentChildrenCheck.EnsureMyChild(children.Select(c => c.Id).ToList(), command.StudentId);
            var child = children.First(c => c.Id == command.StudentId);
            var parentId = _currentUser.ParentId!.Value;

            // One waiting request per student, teacher and subject (the parent asks about one subject at a time)
            var studentId = command.StudentId;
            var teacherId = command.TeacherId;
            var subjectId = command.SubjectId;
            var pending = await _repository.SearchAsync(r => r.StudentId == studentId && r.TeacherId == teacherId
                && r.SubjectId == subjectId && r.Status == EvaluationRequestStatus.Pending);
            if (pending.Count > 0)
                throw new DomainException("There is already a pending evaluation request for this student to this teacher.");

            var entity = EvaluationRequest.Create(studentId, parentId, teacherId, command.SubjectId, DateTime.UtcNow, command.Note,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            await _repository.AddAsync(entity);

            // The teacher gets a notification (Notification service, through Kafka; saved with the request)
            _outbox.Enqueue(Topics.AttendanceEvaluationRequested, entity.Id.ToString(), new EvaluationRequestedIntegrationEvent
            {
                RequestId = entity.Id,
                TeacherId = entity.TeacherId,
                StudentId = entity.StudentId,
                StudentNameAr = child.FullNameAr,
                StudentNameEn = child.FullNameEn,
                SubjectId = entity.SubjectId,
                Note = entity.Note,
                RequestedAt = entity.RequestedAt,
                AcademicYearId = child.AcademicYearId == Guid.Empty ? null : child.AcademicYearId,
                RequestedBy = _currentUser.UserId
            });

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
