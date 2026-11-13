using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class CreateSchoolEventCommandHandler : ICommandHandler<CreateSchoolEventCommand, Guid>
    {
        private readonly ISchoolEventRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public CreateSchoolEventCommandHandler(ICurrentUserService currentUser, ISchoolEventRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateSchoolEventCommand command, CancellationToken cancellationToken)
        {
            var entity = new Domain.SchoolEvent(
                command.Title,
                command.Description,
                command.EventDate,
                command.StartTime,
                command.EndTime,
                command.ImageUrl,
                command.EventFor,
                command.AcademicYearId,
                command.SemesterId,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

            foreach (var target in command.Targets ?? [])
            {
                entity.AddTarget(entity.Id, target.GradeId, target.SectionId, target.TeacherId, target.StudentId,
                    new AuditInfo(DateTime.Now, _currentUser.UserId));
            }

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
