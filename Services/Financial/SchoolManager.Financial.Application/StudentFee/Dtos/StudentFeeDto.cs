using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class StudentFeeDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = string.Empty;
        public FeeStatus Status { get; set; }
        public DateTime DueDate { get; set; }
        public decimal WaivedAmount { get; set; }
        public string? WaiveReason { get; set; }
        public DateTime? WaivedAt { get; set; }
    }
}
