using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class UpdateSemesterCommandHandler : ICommandHandler<UpdateSemesterCommand, bool>
	{
		private readonly ISemesterRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		private readonly ICurrentUserService _currentUser;

		public UpdateSemesterCommandHandler(ICurrentUserService currentUser, ISemesterRepository repository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(UpdateSemesterCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			entity.Update(
				request.Name,
                request.AcademicYearId,
                request.StartDate,
                request.EndDate,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
