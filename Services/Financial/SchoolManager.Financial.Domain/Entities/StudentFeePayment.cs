using BuildingBlocks;

namespace SchoolManager.Financial.Domain;

/// <summary>
/// One payment applied to a student fee (Apply payment in the admin site), with its date: the income report counts
/// collections by date from these. IsBusFee copies whether the fee is a bus fee (StudentFee.BusAssignmentId).
/// </summary>
public sealed class StudentFeePayment : BaseEntity
{
    public Guid StudentFeeId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid? AcademicYearId { get; private set; }
    public bool IsBusFee { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTime PaidOn { get; private set; }
    public Guid RecordedBy { get; private set; }

    private StudentFeePayment() { }

    public StudentFeePayment(StudentFee fee, decimal amount, string currency, DateTime paidOn, Guid recordedBy)
    {
        StudentFeeId = fee.Id;
        StudentId = fee.StudentId;
        AcademicYearId = fee.AcademicYearId;
        IsBusFee = fee.BusAssignmentId is not null;
        Amount = amount;
        Currency = currency.ToUpperInvariant();
        PaidOn = paidOn;
        RecordedBy = recordedBy;
    }
}
