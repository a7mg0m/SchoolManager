using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>Deletes the employee together with its User and UserProfile.</summary>
    public class DeleteEmployeeCommandHandler : ICommandHandler<DeleteEmployeeCommand, bool>
    {
        private readonly IEmployeeRepository _repository;
        private readonly EmployeeAccountService _accounts;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteEmployeeCommandHandler(IEmployeeRepository repository, EmployeeAccountService accounts, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _accounts = accounts;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteEmployeeCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null)
                return false;

            await _accounts.DeleteAsync(entity.UserId);
            _repository.Remove(entity);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
