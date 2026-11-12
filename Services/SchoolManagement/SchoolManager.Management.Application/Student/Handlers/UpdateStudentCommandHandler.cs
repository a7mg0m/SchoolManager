using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class UpdateStudentCommandHandler : ICommandHandler<UpdateStudentCommand, bool>
	{
		private readonly IStudentRepository _repository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IGradeRepository _gradeRepository;

		public UpdateStudentCommandHandler(IStudentRepository repository, IGradeRepository gradeRepository, IUnitOfWork unitOfWork)
		{
			_gradeRepository = gradeRepository;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			if (entity is null)
				return false;

			var grade = await _gradeRepository.GetByIdAsync(request.GradeId)
				?? throw new InvalidOperationException("Grade not found.");

			entity.Update(
				new NameLocalized(request.FullNameAr, request.FullNameEn),
                request.PhoneNumber,
                request.Email,
                request.Address,
                grade.StageId,
                request.GradeId,
                request.SectionId,
                request.AcademicYearId,
                new AuditInfo(DateTime.Now, request.UpdatedBy));

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
