using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class ExpenseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid EventId { get; set; }
        public Guid CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public ExpenseType Type { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; }
        public string? Reference { get; set; }
    }
}
