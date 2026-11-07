using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class CreatePaymentCommandHandler : ICommandHandler<CreatePaymentCommand, Guid>
    {
        private readonly IPaymentRepository _repository;
        private readonly IStudentFeeRepository _studentFeeRepository;
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreatePaymentCommandHandler(
            IPaymentRepository repository,
            IStudentFeeRepository studentFeeRepository,
            IPaymentMethodRepository paymentMethodRepository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _studentFeeRepository = studentFeeRepository;
            _paymentMethodRepository = paymentMethodRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreatePaymentCommand command, CancellationToken cancellationToken)
        {
            var studentFee = await _studentFeeRepository.GetByIdAsync(command.StudentFeeId)
                ?? throw new DomainException("Student fee not found.");
            var method = await _paymentMethodRepository.GetByIdAsync(command.PaymentMethodId)
                ?? throw new DomainException("Payment method not found.");

            var entity = Payment.Create(studentFee.Id, studentFee.StudentId, command.ParentId, method.Id, new Money(command.Amount, command.Currency));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }

    public class CompletePaymentCommandHandler : ICommandHandler<CompletePaymentCommand, bool>
    {
        private readonly IPaymentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CompletePaymentCommandHandler(IPaymentRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(CompletePaymentCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            entity.Complete(command.TransactionReference);

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }

    public class FailPaymentCommandHandler : ICommandHandler<FailPaymentCommand, bool>
    {
        private readonly IPaymentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public FailPaymentCommandHandler(IPaymentRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(FailPaymentCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            entity.Fail(command.Reason);

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }

    public class DeletePaymentCommandHandler : ICommandHandler<DeletePaymentCommand, bool>
    {
        private readonly IPaymentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeletePaymentCommandHandler(IPaymentRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeletePaymentCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
