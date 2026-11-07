namespace SchoolManager.Financial.Application
{
    public class CreatePaymentDto
    {
        public Guid StudentFeeId { get; set; }
        public Guid ParentId { get; set; }
        public Guid PaymentMethodId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
