using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    /// <summary>
    /// Keeps a student's bus fee (a Transportation student fee) in sync with the Attendance service's bus
    /// assignments, from Kafka events on the "attendance.bus-fees" topic. The fee is per student, not taken from
    /// the grade fees.
    ///
    /// - Assigned: create the student fee of the assignment (once).
    /// - FeeChanged: change its amount (payments already made stay); created first when it is missing.
    /// - Ended: the student stops riding the bus. Nothing paid yet: the fee is removed. Partly paid: the unpaid
    ///   rest is waived (the payments stay). Already paid / waived: kept as it is.
    ///
    /// Messages may arrive more than once, so everything is idempotent (one student fee per bus assignment).
    /// A change the fee rules refuse (e.g. lower than what is already paid) is logged and skipped.
    /// </summary>
    public class BusFeeIntegrationEventHandler : IIntegrationEventHandler<BusFeeIntegrationEvent>
    {
        /// <summary>Due date when the event has none</summary>
        private static readonly TimeSpan DefaultDueIn = TimeSpan.FromDays(30);

        /// <summary>Waive reason of the unpaid rest when the bus assignment ends</summary>
        private const string EndedReason = "Bus assignment ended";

        private readonly IStudentFeeRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BusFeeIntegrationEventHandler> _logger;

        public BusFeeIntegrationEventHandler(IStudentFeeRepository repository, IUnitOfWork unitOfWork, ILogger<BusFeeIntegrationEventHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleAsync(BusFeeIntegrationEvent evt, CancellationToken cancellationToken)
        {
            if (evt.BusAssignmentId == Guid.Empty || evt.StudentId == Guid.Empty)
            {
                _logger.LogWarning("Bus fee event {EventId} has no bus assignment / student, skipped", evt.EventId);
                return;
            }

            var assignmentId = evt.BusAssignmentId;
            var fee = (await _repository.SearchAsync(f => f.BusAssignmentId == assignmentId)).FirstOrDefault();

            try
            {
                switch (evt.ChangeType)
                {
                    case BusFeeChangeType.Assigned:
                        if (fee is not null)
                            return; // already created (message delivered again)
                        await _repository.AddAsync(Create(evt));
                        break;

                    case BusFeeChangeType.FeeChanged:
                        if (fee is null)
                        {
                            await _repository.AddAsync(Create(evt));
                            break;
                        }
                        fee.ChangeBusFee(new Money(evt.Amount, evt.Currency), DateTime.UtcNow);
                        await _repository.UpdateAsync(fee);
                        break;

                    case BusFeeChangeType.Ended:
                        if (fee is null)
                            return; // never created or already removed
                        if (fee.PaidAmount.Amount == 0 && fee.WaivedAmount == 0)
                        {
                            // Nothing paid: no bus fee for an assignment that ended
                            _repository.Remove(fee);
                            break;
                        }
                        if (fee.Status is FeeStatus.Paid or FeeStatus.Waived || fee.Balance.Amount == 0)
                            return; // settled: kept as it is
                        // Partly paid: the payments stay, the unpaid rest is forgiven
                        fee.Waive(EndedReason, DateTime.UtcNow);
                        await _repository.UpdateAsync(fee);
                        break;

                    default:
                        _logger.LogWarning("Bus fee event {EventId}: unknown change type {ChangeType}, skipped", evt.EventId, evt.ChangeType);
                        return;
                }
            }
            catch (DomainException ex)
            {
                _logger.LogWarning("Bus fee event {EventId} for assignment {AssignmentId} refused: {Reason}", evt.EventId, assignmentId, ex.Message);
                return;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static StudentFee Create(BusFeeIntegrationEvent evt)
            => StudentFee.AssignBusFee(evt.StudentId, new Money(evt.Amount, evt.Currency),
                evt.DueDate ?? DateTime.Now.Add(DefaultDueIn), evt.AcademicYearId, evt.BusAssignmentId);
    }
}
