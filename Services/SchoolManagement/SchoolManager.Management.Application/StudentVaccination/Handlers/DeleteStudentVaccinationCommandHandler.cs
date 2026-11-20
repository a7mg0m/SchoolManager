using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class DeleteStudentVaccinationCommandHandler : ICommandHandler<DeleteStudentVaccinationCommand, bool>
	{
		private readonly IStudentVaccinationRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public DeleteStudentVaccinationCommandHandler(IStudentVaccinationRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(DeleteStudentVaccinationCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			_repository.Remove(entity);

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
