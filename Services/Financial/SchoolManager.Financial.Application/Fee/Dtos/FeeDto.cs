using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class FeeDto
    {
        public Guid Id { get; set; }
        public Guid GradeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public FeeType Type { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
    }
}
