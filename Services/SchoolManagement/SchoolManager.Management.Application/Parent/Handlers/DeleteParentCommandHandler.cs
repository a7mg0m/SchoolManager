using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class DeleteParentCommandHandler : ICommandHandler<DeleteParentCommand, bool>
	{
		private readonly IParentRepository _repository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IStudentRepository _studentRepository;

		public DeleteParentCommandHandler(IParentRepository repository, IStudentRepository studentRepository, IUnitOfWork unitOfWork)
		{
			_studentRepository = studentRepository;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(DeleteParentCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdWithStudentsAsync(request.Id);

			if (entity is null)
				return false;

			foreach (var student in entity.Students.ToList())
				_studentRepository.Remove(student);

			_repository.Remove(entity);

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
