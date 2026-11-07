using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class CreateRevenueDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public RevenueType Type { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime RevenueDate { get; set; }
        public string? Reference { get; set; }
    }
}
