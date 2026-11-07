using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class UpdateFeeDto
    {
        public string Name { get; set; } = string.Empty;
        public FeeType Type { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
    }
}
