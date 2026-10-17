using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class CreateHomeworkCommandHandler : ICommandHandler<CreateHomeworkCommand, Guid>
    {
        private readonly IHomeworkRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateHomeworkCommandHandler(IHomeworkRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateHomeworkCommand command, CancellationToken cancellationToken)
        {
            var entity = new Homework(
                command.SubjectId,
                command.TeacherId,
                command.GradeId,
                command.AcademicYearId,
                command.SemesterId,
                command.Title,
                command.Description,
                command.DueDate,
                command.AttachmentUrl,
                new AuditInfo(DateTime.Now, command.CreatedBy));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
