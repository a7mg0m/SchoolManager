using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class UpdateSubjectCommandHandler : ICommandHandler<UpdateSubjectCommand, bool>
	{
		private readonly ISubjectRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		private readonly ICurrentUserService _currentUser;

		public UpdateSubjectCommandHandler(ICurrentUserService currentUser, ISubjectRepository repository, IUnitOfWork unitOfWork)
		{
			_currentUser = currentUser;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(UpdateSubjectCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdWithTeachersAsync(request.Id);

			if (entity is null)
				return false;
			entity.Update(
				new NameLocalized(request.NameAr, request.NameEn),
                request.StageId,
                request.GradeId,
                request.TeacherIds,
                new AuditInfo(DateTime.Now, _currentUser.UserId));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
