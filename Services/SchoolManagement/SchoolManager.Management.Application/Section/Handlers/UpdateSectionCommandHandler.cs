using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class UpdateSectionCommandHandler : ICommandHandler<UpdateSectionCommand, bool>
	{
		private readonly ISectionRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		private readonly ICurrentUserService _currentUser;

		public UpdateSectionCommandHandler(ICurrentUserService currentUser, ISectionRepository repository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(UpdateSectionCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			entity.Update(
				new NameLocalized(request.NameAr, request.NameEn),
                request.GradeId,
                request.StageId,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
