using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class SaveStudentMedicalProfileCommandHandler : ICommandHandler<SaveStudentMedicalProfileCommand, Guid>
	{
		private readonly IStudentMedicalProfileRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		private readonly ICurrentUserService _currentUser;

		public SaveStudentMedicalProfileCommandHandler(ICurrentUserService currentUser, IStudentMedicalProfileRepository repository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(SaveStudentMedicalProfileCommand command, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByStudentIdAsync(command.StudentId);

			if (entity is null)
			{
				entity = new StudentMedicalProfile(
					command.StudentId,
					command.BloodType,
					command.Allergies,
					command.ChronicDiseases,
					command.RegularMedications,
					command.EmergencyPhone,
					command.Notes,
					new AuditInfo(DateTime.Now, _currentUser.UserId));

				await _repository.AddAsync(entity);
			}
			else
			{
				entity.Update(
					command.BloodType,
					command.Allergies,
					command.ChronicDiseases,
					command.RegularMedications,
					command.EmergencyPhone,
					command.Notes,
					new AuditInfo(DateTime.Now, _currentUser.UserId));
			}

			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
