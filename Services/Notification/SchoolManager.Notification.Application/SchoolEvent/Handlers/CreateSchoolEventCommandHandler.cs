using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class CreateSchoolEventCommandHandler : ICommandHandler<CreateSchoolEventCommand, Guid>
    {
        private readonly ISchoolEventRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateSchoolEventCommandHandler(ISchoolEventRepository repository, IUnitOfWork unitOfWork)
        {
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
                new AuditInfo(DateTime.Now, command.CreatedBy));

            foreach (var target in command.Targets ?? [])
            {
                entity.AddTarget(entity.Id, target.GradeId, target.SectionId, target.TeacherId, target.StudentId,
                    new AuditInfo(DateTime.Now, command.CreatedBy));
            }

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
