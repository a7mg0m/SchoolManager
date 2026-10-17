using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class UpdateTimetableCommandHandler : ICommandHandler<UpdateTimetableCommand, bool>
	{
		private readonly ITimetableRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public UpdateTimetableCommandHandler(ITimetableRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(UpdateTimetableCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			entity.Update(
				request.GradeId,
                request.AcademicYearId,
                new AuditInfo(DateTime.Now, request.UpdatedBy));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
