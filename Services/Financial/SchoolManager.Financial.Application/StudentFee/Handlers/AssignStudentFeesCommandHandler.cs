using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class AssignStudentFeesCommandHandler : ICommandHandler<AssignStudentFeesCommand, List<Guid>>
    {
        private readonly IStudentFeeRepository _repository;
        private readonly IFeesRepository _feesRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AssignStudentFeesCommandHandler(
            IStudentFeeRepository repository,
            IFeesRepository feesRepository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _feesRepository = feesRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<Guid>> Handle(AssignStudentFeesCommand command, CancellationToken cancellationToken)
        {
            var studentIds = (command.StudentIds ?? new()).Where(id => id != Guid.Empty).Distinct().ToList();
            var feeIds = (command.FeeIds ?? new()).Where(id => id != Guid.Empty).Distinct().ToList();

            if (studentIds.Count == 0)
                throw new DomainException("Select at least one student.");
            if (feeIds.Count == 0)
                throw new DomainException("Select at least one fee.");
            if (command.DueDate == default)
                throw new DomainException("Due date is required.");

            var fees = await _feesRepository.SearchAsync(f => feeIds.Contains(f.Id));
            if (fees.Count != feeIds.Count)
                throw new DomainException("One or more selected fees were not found.");

            var currencies = fees.Select(f => f.Amount.Currency.ToUpperInvariant()).Distinct().ToList();
            if (currencies.Count > 1)
                throw new DomainException($"Selected fees use different currencies ({string.Join(", ", currencies)}). Assign them separately.");

            var currency = fees[0].Amount.Currency;
            var total = new Money(fees.Sum(f => f.Amount.Amount), currency);

            var ids = new List<Guid>();
            foreach (var studentId in studentIds)
            {
                var studentFee = StudentFee.Assign(studentId, total, command.DiscountAmount, command.DueDate);
                await _repository.AddAsync(studentFee);
                ids.Add(studentFee.Id);
            }

            await _unitOfWork.SaveChangesAsync();
            return ids;
        }
    }
}
