using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class UpdateVaccineCommandHandler : ICommandHandler<UpdateVaccineCommand, bool>
	{
		private readonly ICurrentUserService _currentUser;
		private readonly IVaccineRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public UpdateVaccineCommandHandler(ICurrentUserService currentUser, IVaccineRepository repository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(UpdateVaccineCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			entity.Update(
				new NameLocalized(request.NameAr, request.NameEn),
				request.TotalDoses,
				request.IsMandatory,
				new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
