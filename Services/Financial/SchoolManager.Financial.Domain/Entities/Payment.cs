using BuildingBlocks;


namespace SchoolManager.Financial.Domain;

public sealed class Payment : BaseEntity
{
    public Guid StudentFeeId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid ParentId { get; private set; }
    public Guid PaymentMethodId { get; private set; }
    public PaymentMethod? PaymentMethod { get; private set; }
    public Money Amount { get; private set; } = default!;
    public PaymentStatus Status { get; private set; }
    public string? TransactionReference { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private Payment() { }

    public static Payment Create(Guid studentFeeId, Guid studentId, Guid parentId, Guid paymentMethodId, Money amount)
    {

        return new Payment
        {
            StudentFeeId = studentFeeId,
            StudentId = studentId,
            ParentId = parentId,
            PaymentMethodId = paymentMethodId,
            Amount = amount,
            Status = PaymentStatus.Pending
        };
    }

    public void Complete(string transactionReference)
    {
        Status = PaymentStatus.Completed;
        TransactionReference = transactionReference;
        PaidAt = DateTime.UtcNow;
        //Raise(new PaymentCompletedEvent(Id, StudentFeeId, StudentId, ParentId, Amount));
    }

    public void Fail(string reason)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        //Raise(new PaymentFailedEvent(Id, StudentFeeId, StudentId, ParentId, Amount, reason));
    }
}


