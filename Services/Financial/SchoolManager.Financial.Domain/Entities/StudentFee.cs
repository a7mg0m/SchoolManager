using BuildingBlocks;

namespace SchoolManager.Financial.Domain;

public sealed class StudentFee : BaseEntity
{
    public Guid StudentId { get; private set; }
    public Money TotalAmount { get; private set; } = default!;
    public Money DiscountAmount { get; private set; } = default!;
    public Money PaidAmount { get; private set; } = default!;
    public FeeStatus Status { get; private set; }
    public DateTime DueDate { get; private set; }

    /// <summary>Unpaid balance forgiven by <see cref="Waive"/> (same currency as TotalAmount).</summary>
    public decimal WaivedAmount { get; private set; }
    public string? WaiveReason { get; private set; }
    public DateTime? WaivedAt { get; private set; }

    // Derived values (not stored – see StudentFeeConfiguration: Ignore)
    public Money NetAmount => new(Math.Max(TotalAmount.Amount - DiscountAmount.Amount, 0), TotalAmount.Currency);
    public Money Balance => new(Math.Max(NetAmount.Amount - PaidAmount.Amount - WaivedAmount, 0), TotalAmount.Currency);

    private StudentFee() { }

    /// <summary>
    /// Assigns fees to a student. <paramref name="total"/> is the sum of the assigned fees.
    /// </summary>
    public static StudentFee Assign(Guid studentId, Money total, decimal discount, DateTime dueDate)
    {
        if (studentId == Guid.Empty)
            throw new DomainException("Student is required.");
        if (total.Amount <= 0)
            throw new DomainException("Total amount must be greater than zero.");
        if (discount < 0)
            throw new DomainException("Discount cannot be negative.");
        if (discount > total.Amount)
            throw new DomainException("Discount cannot be greater than the total amount.");

        var fee = new StudentFee
        {
            StudentId = studentId,
            // Copy: EF Core owned types are tracked by reference, so a Money instance
            // shared between several StudentFee rows would be saved as NULL on all but one.
            TotalAmount = new Money(total.Amount, total.Currency),
            DiscountAmount = new Money(discount, total.Currency),
            PaidAmount = Money.Zero(total.Currency),
            DueDate = dueDate
        };

        // A 100% discount leaves nothing to pay
        fee.Status = fee.NetAmount.Amount == 0 ? FeeStatus.Waived : FeeStatus.Pending;
        return fee;
    }

    public void ApplyPayment(Money amount)
    {
        if (Status is FeeStatus.Paid or FeeStatus.Waived)
            throw new DomainException("This fee has no remaining balance.");
        if (amount.Amount <= 0)
            throw new DomainException("Payment amount must be greater than zero.");
        if (!string.Equals(amount.Currency, TotalAmount.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException($"Payment currency must be {TotalAmount.Currency}.");
        if (amount.Amount > Balance.Amount)
            throw new DomainException("Payment cannot exceed the remaining balance.");

        PaidAmount = new Money(PaidAmount.Amount + amount.Amount, TotalAmount.Currency);
        Status = Balance.Amount == 0 ? FeeStatus.Paid : FeeStatus.PartiallyPaid;
    }

    /// <summary>
    /// Forgives the remaining balance. Payments already made stay recorded;
    /// only the unpaid part is waived.
    /// </summary>
    public void Waive(string reason, DateTime waivedAt)
    {
        if (Status is FeeStatus.Waived)
            throw new DomainException("This fee is already waived.");
        if (Status is FeeStatus.Paid || Balance.Amount == 0)
            throw new DomainException("This fee is fully paid; there is nothing to waive.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("A reason is required to waive a fee.");

        reason = reason.Trim();
        if (reason.Length > MaxWaiveReasonLength)
            throw new DomainException($"Reason cannot exceed {MaxWaiveReasonLength} characters.");

        WaivedAmount = Balance.Amount;
        WaiveReason = reason;
        WaivedAt = waivedAt;
        Status = FeeStatus.Waived;
    }

    /// <summary>
    /// Undoes <see cref="Waive"/>: the waived balance becomes payable again.
    /// </summary>
    public void RestoreWaiver()
    {
        if (Status is not FeeStatus.Waived)
            throw new DomainException("This fee is not waived.");
        if (WaivedAmount == 0)
            throw new DomainException("This fee was waived by a full discount at assignment and cannot be restored.");

        WaivedAmount = 0;
        WaiveReason = null;
        WaivedAt = null;
        Status = Balance.Amount == 0
            ? FeeStatus.Paid
            : PaidAmount.Amount > 0 ? FeeStatus.PartiallyPaid : FeeStatus.Pending;
    }

    public const int MaxWaiveReasonLength = 500;

    public void MarkOverdueIfDue(DateTime utcNow)
    {
        if (Status is FeeStatus.Pending or FeeStatus.PartiallyPaid && DueDate < utcNow)
            Status = FeeStatus.Overdue;
    }
}
