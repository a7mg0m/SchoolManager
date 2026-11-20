using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class UpdateClinicVisitCommandHandler : ICommandHandler<UpdateClinicVisitCommand, bool>
	{
		private readonly ICurrentUserService _currentUser;
		private readonly IClinicVisitRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public UpdateClinicVisitCommandHandler(ICurrentUserService currentUser, IClinicVisitRepository repository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(UpdateClinicVisitCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdWithMedicationsAsync(request.Id);

			if (entity is null)
				return false;

			entity.Update(
				request.DoctorName.Trim(),
				request.VisitDate,
				request.Complaint.Trim(),
				request.Temperature,
				request.Weight,
				request.Height,
				request.Diagnosis,
				request.Treatment,
				request.Outcome,
				request.ParentNotified,
				request.Notes,
				request.Medications.Select(m => (m.MedicineName.Trim(), m.Dose.Trim())),
				new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
