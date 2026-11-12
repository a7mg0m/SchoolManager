using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class UpdateParentCommandHandler : ICommandHandler<UpdateParentCommand, bool>
	{
		private readonly IParentRepository _repository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IGradeRepository _gradeRepository;
		private readonly IStudentRepository _studentRepository;

		public UpdateParentCommandHandler(IParentRepository repository, IStudentRepository studentRepository, IGradeRepository gradeRepository, IUnitOfWork unitOfWork)
		{
			_gradeRepository = gradeRepository;
			_studentRepository = studentRepository;
			_repository = repository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> Handle(UpdateParentCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdWithStudentsAsync(request.Id);

			if (entity is null)
				return false;

			entity.Update(
				new NameLocalized(request.FullNameAr, request.FullNameEn),
                request.PhoneNumber,
                request.Email,
                request.Address,
                new AuditInfo(DateTime.Now, request.UpdatedBy));

            AuditInfo NewAudit() => new AuditInfo(DateTime.Now, request.UpdatedBy);
            var incomingIds = request.Students.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();

            foreach (var removed in entity.Students.Where(s => !incomingIds.Contains(s.Id)).ToList())
            {
                entity.Students.Remove(removed);
                _studentRepository.Remove(removed);
            }

            foreach (var s in request.Students)
            {
                var grade = await _gradeRepository.GetByIdAsync(s.GradeId)
                    ?? throw new InvalidOperationException("Grade not found.");
                var existing = s.Id.HasValue ? entity.Students.FirstOrDefault(x => x.Id == s.Id.Value) : null;

                if (existing is not null)
                {
                    existing.Update(
                        new NameLocalized(s.FullNameAr, s.FullNameEn),
                        s.PhoneNumber,
                        s.Email,
                        s.Address,
                        grade.StageId,
                        s.GradeId,
                        s.SectionId,
                        s.AcademicYearId,
                        NewAudit());
                }
                else
                {
                    await _studentRepository.AddAsync(new Student(
                        new NameLocalized(s.FullNameAr, s.FullNameEn),
                        s.DateOfBirth,
                        s.NationalId,
                        s.Gender,
                        s.PhoneNumber,
                        s.Email,
                        s.Address,
                        grade.StageId,
                        s.GradeId,
                        s.SectionId,
                        s.AcademicYearId,
                        entity.Id,
                        NewAudit()));
                }
            }

			await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
