using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateStudentVaccinationCommandHandler : ICommandHandler<CreateStudentVaccinationCommand, Guid>
	{
		private readonly ICurrentUserService _currentUser;
		private readonly IStudentVaccinationRepository _repository;
		private readonly IVaccineRepository _vaccineRepository;
		private readonly IUnitOfWork _unitOfWork;

		public CreateStudentVaccinationCommandHandler(ICurrentUserService currentUser, IStudentVaccinationRepository repository, IVaccineRepository vaccineRepository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_vaccineRepository = vaccineRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateStudentVaccinationCommand command, CancellationToken cancellationToken)
		{
			await StudentVaccinationException.CheckAsync(null, command.StudentId, command.VaccineId, command.DoseNumber,
				_vaccineRepository, _repository);

			var entity = new StudentVaccination(
				command.StudentId,
				command.VaccineId,
				command.DoseNumber,
				command.Status,
				command.VaccinationDate,
				command.Place,
				command.CampaignId,
				command.GivenByEmployeeId,
				command.Notes,
				new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
