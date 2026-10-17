using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class CreateTeacherCommandHandler : ICommandHandler<CreateTeacherCommand, Guid>
	{
		private readonly ITeacherRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public CreateTeacherCommandHandler(ITeacherRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> Handle(CreateTeacherCommand command, CancellationToken cancellationToken)
		{
			var entity = new Teacher(
				new NameLocalized(command.FullNameAr, command.FullNameEn),
                command.DateOfBirth,
                command.NationalId,
                command.Gender,
                command.PhoneNumber,
                command.Email,
                command.Address,
                new AuditInfo(DateTime.Now, command.CreatedBy));

			await _repository.AddAsync(entity);
			await _unitOfWork.SaveChangesAsync();

			return entity.Id;
		}
	}
}
