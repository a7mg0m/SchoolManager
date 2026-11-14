using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class DeleteEmployeeGroupCommandHandler : ICommandHandler<DeleteEmployeeGroupCommand, bool>
    {
        private readonly IEmployeeGroupRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteEmployeeGroupCommandHandler(IEmployeeGroupRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteEmployeeGroupCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null)
                return false;

            if (await _repository.HasEmployeesAsync(command.Id))
                throw new DomainException(ErrorKeys.EmployeeGroupHasEmployees);

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
