using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class ApplyStudentFeePaymentCommandHandler : ICommandHandler<ApplyStudentFeePaymentCommand, bool>
    {
        private readonly IStudentFeeRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public ApplyStudentFeePaymentCommandHandler(IStudentFeeRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(ApplyStudentFeePaymentCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            entity.ApplyPayment(new Money(command.Amount, command.Currency));

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }

    public class WaiveStudentFeeCommandHandler : ICommandHandler<WaiveStudentFeeCommand, bool>
    {
        private readonly IStudentFeeRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public WaiveStudentFeeCommandHandler(IStudentFeeRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(WaiveStudentFeeCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            entity.Waive(command.Reason, DateTime.UtcNow);

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }

    public class RestoreStudentFeeWaiverCommandHandler : ICommandHandler<RestoreStudentFeeWaiverCommand, bool>
    {
        private readonly IStudentFeeRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public RestoreStudentFeeWaiverCommandHandler(IStudentFeeRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(RestoreStudentFeeWaiverCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            entity.RestoreWaiver();

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }

    public class DeleteStudentFeeCommandHandler : ICommandHandler<DeleteStudentFeeCommand, bool>
    {
        private readonly IStudentFeeRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteStudentFeeCommandHandler(IStudentFeeRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteStudentFeeCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
