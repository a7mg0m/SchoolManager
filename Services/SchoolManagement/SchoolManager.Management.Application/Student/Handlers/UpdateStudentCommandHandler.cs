using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class UpdateStudentCommandHandler : ICommandHandler<UpdateStudentCommand, bool>
	{
		private readonly IStudentRepository _repository;
		private readonly IUnitOfWork _unitOfWork;

		public UpdateStudentCommandHandler(IStudentRepository repository, IUnitOfWork unitOfWork)
		{
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			entity.Update(
				new NameLocalized(request.FullNameAr, request.FullNameEn),
                request.PhoneNumber,
                request.Email,
                request.Address,
                request.GradeId,
                request.SectionId,
                request.AcademicYearId,
                new AuditInfo(DateTime.Now, request.UpdatedBy));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
