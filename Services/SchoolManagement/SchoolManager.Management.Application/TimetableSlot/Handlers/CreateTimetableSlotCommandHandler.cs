using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateTimetableSlotCommandHandler : ICommandHandler<CreateTimetableSlotCommand, Guid>
	{
		private readonly ITimetableSlotRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public CreateTimetableSlotCommandHandler(ITimetableSlotRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateTimetableSlotCommand command, CancellationToken cancellationToken)
		{
			var entity = new TimetableSlot(
				command.Day,
                new NameLocalized(command.NameAr, command.NameEn),
                command.StartTime,
                command.EndTime,
                command.SubjectId,
                command.TeacherId,
                command.GradeId,
                command.SectionId,
                new AuditInfo(DateTime.Now, command.CreatedBy));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
