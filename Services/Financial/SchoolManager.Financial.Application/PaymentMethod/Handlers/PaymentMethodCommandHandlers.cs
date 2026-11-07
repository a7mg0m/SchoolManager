using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class CreatePaymentMethodCommandHandler : ICommandHandler<CreatePaymentMethodCommand, Guid>
    {
        private readonly IPaymentMethodRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreatePaymentMethodCommandHandler(IPaymentMethodRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreatePaymentMethodCommand command, CancellationToken cancellationToken)
        {
            var entity = PaymentMethod.Create(
                command.Name,
                command.Type,
                new AuditInfo(DateTime.Now, command.CreatedBy));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }

    public class UpdatePaymentMethodCommandHandler : ICommandHandler<UpdatePaymentMethodCommand, bool>
    {
        private readonly IPaymentMethodRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdatePaymentMethodCommandHandler(IPaymentMethodRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdatePaymentMethodCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            entity.Update(command.Name, command.Type);

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }

    public class DeletePaymentMethodCommandHandler : ICommandHandler<DeletePaymentMethodCommand, bool>
    {
        private readonly IPaymentMethodRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeletePaymentMethodCommandHandler(IPaymentMethodRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeletePaymentMethodCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
