using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class DeleteAcademicYearCommandHandler : ICommandHandler<DeleteAcademicYearCommand, bool>
	{
		private readonly IAcademicYearRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public DeleteAcademicYearCommandHandler(IAcademicYearRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(DeleteAcademicYearCommand request, CancellationToken cancellationToken)
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
