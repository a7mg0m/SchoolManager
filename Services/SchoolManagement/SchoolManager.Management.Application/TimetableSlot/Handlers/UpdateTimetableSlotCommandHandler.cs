using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class UpdateTimetableSlotCommandHandler : ICommandHandler<UpdateTimetableSlotCommand, bool>
	{
		private readonly ITimetableSlotRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public UpdateTimetableSlotCommandHandler(ITimetableSlotRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(UpdateTimetableSlotCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			entity.Update(
				request.Day,
                new NameLocalized(request.NameAr, request.NameEn),
                request.StartTime,
                request.EndTime,
                request.GradeId,
                request.SectionId,
                request.SubjectId,
                request.TeacherId,
                new AuditInfo(DateTime.Now, request.UpdatedBy));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
