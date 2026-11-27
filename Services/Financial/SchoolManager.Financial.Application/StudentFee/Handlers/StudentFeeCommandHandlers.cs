using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class ApplyStudentFeePaymentCommandHandler : ICommandHandler<ApplyStudentFeePaymentCommand, bool>
    {
        private readonly IStudentFeeRepository _repository;
        private readonly IStudentFeePaymentRepository _payments;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIntegrationEventOutbox _outbox;

        public ApplyStudentFeePaymentCommandHandler(
            IStudentFeeRepository repository,
            IStudentFeePaymentRepository payments,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork,
            IIntegrationEventOutbox outbox)
        {
            _outbox = outbox;
            _repository = repository;
            _payments = payments;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(ApplyStudentFeePaymentCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            entity.ApplyPayment(new Money(command.Amount, command.Currency));
            // Dated record of the payment (income report)
            var payment = new StudentFeePayment(entity, command.Amount, command.Currency, DateTime.Now, _currentUser.UserId);
            await _payments.AddAsync(payment);

            // The student's parents get a notification (Notification service, through Kafka; saved with the payment)
            _outbox.Enqueue(Topics.FinancialFeePaymentRecorded, entity.Id.ToString(), new FeePaymentRecordedIntegrationEvent
            {
                PaymentId = payment.Id,
                StudentFeeId = entity.Id,
                StudentId = entity.StudentId,
                AcademicYearId = entity.AcademicYearId,
                IsBusFee = payment.IsBusFee,
                Amount = payment.Amount,
                Currency = payment.Currency,
                PaidOn = payment.PaidOn,
                Balance = entity.Balance.Amount,
                RecordedBy = _currentUser.UserId
            });

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
