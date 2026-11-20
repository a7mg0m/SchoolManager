using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateClinicVisitCommandHandler : ICommandHandler<CreateClinicVisitCommand, Guid>
	{
		private readonly ICurrentUserService _currentUser;
		private readonly IClinicVisitRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public CreateClinicVisitCommandHandler(ICurrentUserService currentUser, IClinicVisitRepository repository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateClinicVisitCommand command, CancellationToken cancellationToken)
		{
			var entity = new ClinicVisit(
				command.StudentId,
				command.DoctorName.Trim(),
				command.VisitDate,
				command.Complaint.Trim(),
				command.Temperature,
				command.Weight,
				command.Height,
				command.Diagnosis,
				command.Treatment,
				command.Outcome,
				command.ParentNotified,
				command.Notes,
				command.Medications.Select(m => (m.MedicineName.Trim(), m.Dose.Trim())),
				new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
