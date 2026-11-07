

using BuildingBlocks;

namespace SchoolManager.Financial.Domain;



public sealed class StudentFee : BaseEntity
{
    public Guid StudentId { get; private set; }
    public Money TotalAmount { get; private set; } 
    public Money DiscountAmount { get; private set; }
    public Money PaidAmount { get; private set; } 
    public FeeStatus Status { get; private set; }
    public DateTime DueDate { get; private set; }

    public Money NetAmount { get; private set; }
    public Money Balance { get; private set; }

    private StudentFee() { }

    public static StudentFee Assign(Guid studentId,  decimal discount, DateTime dueDate)
    {
        var discountMoney = new Money(discount, "EGP");

        return new StudentFee
        {
            StudentId = studentId,
            TotalAmount = Money.Zero("EGP"),
            DiscountAmount = discountMoney,
            PaidAmount = Money.Zero("EGP"),
            Status = FeeStatus.Pending,
            DueDate = dueDate
        };
    }

    public void ApplyPayment(Money amount)
    {
       
        PaidAmount = PaidAmount.Add(amount);
        Balance = Balance.Subtract(amount);
        Status = Balance.Amount==0 ? FeeStatus.Paid : FeeStatus.PartiallyPaid;
    }

    public void MarkOverdueIfDue(DateTime utcNow)
    {
        if (Status is FeeStatus.Pending or FeeStatus.PartiallyPaid && DueDate < utcNow)
            Status = FeeStatus.Overdue;
    }
}
