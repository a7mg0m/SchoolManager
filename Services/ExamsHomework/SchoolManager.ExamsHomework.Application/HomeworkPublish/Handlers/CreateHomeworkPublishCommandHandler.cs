using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class CreateHomeworkPublishCommandHandler : ICommandHandler<CreateHomeworkPublishCommand, Guid>
    {
        private readonly IHomeworkPublishRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUser;

        public CreateHomeworkPublishCommandHandler(ICurrentUserService currentUser, IHomeworkPublishRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateHomeworkPublishCommand command, CancellationToken cancellationToken)
        {
            var entity = new HomeworkPublish(
                command.HomeworkId,
                command.GradeId,
                command.SectionId,
                command.Status,
                _currentUser.UserId);

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
