using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class UpdateTimetableCommandHandler : ICommandHandler<UpdateTimetableCommand, bool>
	{
		private readonly ITimetableRepository _repository;
		private readonly ITimetableSlotRepository _slotRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IGradeRepository _gradeRepository;

		private readonly ICurrentUserService _currentUser;

		public UpdateTimetableCommandHandler(ICurrentUserService currentUser, 
			ITimetableRepository repository,
			ITimetableSlotRepository slotRepository,
			IGradeRepository gradeRepository,
			IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_gradeRepository = gradeRepository;
			_repository = repository;
			_slotRepository = slotRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(UpdateTimetableCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			var grade = await _gradeRepository.GetByIdAsync(request.GradeId)
				?? throw new InvalidOperationException("Grade not found.");

			if (grade.StageId != request.StageId)
				throw new InvalidOperationException("Grade does not belong to the selected stage.");

			// AuditInfo is an EF owned type: every entity needs its OWN instance.
			// Sharing one instance across entities leaves the others' audit columns NULL.
			var now = DateTime.Now;
			AuditInfo Updated(AuditInfo? current) => current is null
				? new AuditInfo(now, _currentUser.UserId)
				: new AuditInfo(current.CreatedOn, current.CreatedBy, now, _currentUser.UserId);

			entity.Update(
				request.StageId,
				request.GradeId,
				request.AcademicYearId,
				Updated(entity.AuditInfo));

			var incoming = request.Slots ?? new();
			var incomingIds = incoming.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();

			// Remove slots that are no longer sent
			foreach (var existing in entity.Slots.Where(s => !incomingIds.Contains(s.Id)).ToList())
			{
				entity.RemoveSlot(existing);
				_slotRepository.Remove(existing);
			}

			// Update existing slots / add new ones
			foreach (var dto in incoming)
			{
				var existing = dto.Id.HasValue ? entity.Slots.FirstOrDefault(s => s.Id == dto.Id.Value) : null;
				var name = new NameLocalized(dto.NameAr, dto.NameEn);

				if (existing is not null)
				{
					existing.Update(dto.Day, name, dto.StartTime, dto.EndTime,
						dto.GradeId, dto.SectionId, dto.SubjectId, dto.TeacherId, Updated(existing.AuditInfo));
				}
				else
				{
					var slot = new TimetableSlot(dto.Day, name, dto.StartTime, dto.EndTime,
						dto.SubjectId, dto.TeacherId, dto.GradeId, dto.SectionId, new AuditInfo(now, _currentUser.UserId));

					entity.AddSlot(slot);
					// Mark explicitly as Added (client-generated Guid would otherwise be treated as Modified)
					await _slotRepository.AddAsync(slot);
				}
			}

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
