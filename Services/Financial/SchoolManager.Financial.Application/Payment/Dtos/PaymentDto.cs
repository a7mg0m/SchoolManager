using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid StudentFeeId { get; set; }
        public Guid StudentId { get; set; }
        public Guid ParentId { get; set; }
        public Guid PaymentMethodId { get; set; }
        public string? PaymentMethodName { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public string? TransactionReference { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
