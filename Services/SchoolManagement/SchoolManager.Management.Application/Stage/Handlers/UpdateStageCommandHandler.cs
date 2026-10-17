using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class UpdateStageCommandHandler : ICommandHandler<UpdateStageCommand, bool>
	{
		private readonly IStageRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public UpdateStageCommandHandler(IStageRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(UpdateStageCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			entity.Update(
				new NameLocalized(request.NameAr, request.NameEn),
                new AuditInfo(DateTime.Now, request.UpdatedBy));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}

