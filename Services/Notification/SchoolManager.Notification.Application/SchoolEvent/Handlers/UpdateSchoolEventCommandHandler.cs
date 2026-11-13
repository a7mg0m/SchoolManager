using BuildingBlocks;
using BuildingBlocks.CQRS;
using MediatR;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class UpdateSchoolEventCommandHandler : ICommandHandler<UpdateSchoolEventCommand>
    {
        private readonly ISchoolEventRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public UpdateSchoolEventCommandHandler(ICurrentUserService currentUser, ISchoolEventRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(UpdateSchoolEventCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException($"SchoolEvent {command.Id} not found");

            entity.Update(
                command.Title,
                command.Description,
                command.EventDate,
                command.StartTime,
                command.EndTime,
                command.ImageUrl,
                command.EventFor,
                command.AcademicYearId,
                command.SemesterId,
                new AuditInfo(entity.AuditInfo.CreatedOn, entity.AuditInfo.CreatedBy, DateTime.Now, _currentUser.UserId));

            if (command.Targets is not null)
            {
                entity.ClearTargets();
                foreach (var target in command.Targets)
                {
                    entity.AddTarget(entity.Id, target.GradeId, target.SectionId, target.TeacherId, target.StudentId,
                        new AuditInfo(DateTime.Now, _currentUser.UserId));
                }
            }

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
