using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class UpdateStudentVaccinationCommandHandler : ICommandHandler<UpdateStudentVaccinationCommand, bool>
	{
		private readonly ICurrentUserService _currentUser;
		private readonly IStudentVaccinationRepository _repository;
		private readonly IVaccineRepository _vaccineRepository;
		private readonly IUnitOfWork _unitOfWork;

		public UpdateStudentVaccinationCommandHandler(ICurrentUserService currentUser, IStudentVaccinationRepository repository, IVaccineRepository vaccineRepository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_vaccineRepository = vaccineRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(UpdateStudentVaccinationCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			await StudentVaccinationException.CheckAsync(entity.Id, entity.StudentId, request.VaccineId, request.DoseNumber,
				_vaccineRepository, _repository);

			entity.Update(
				request.VaccineId,
				request.DoseNumber,
				request.Status,
				request.VaccinationDate,
				request.Place,
				request.CampaignId,
				request.GivenByEmployeeId,
				request.Notes,
				new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
