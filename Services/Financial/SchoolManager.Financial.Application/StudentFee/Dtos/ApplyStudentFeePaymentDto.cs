namespace SchoolManager.Financial.Application
{
    public class ApplyStudentFeePaymentDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
