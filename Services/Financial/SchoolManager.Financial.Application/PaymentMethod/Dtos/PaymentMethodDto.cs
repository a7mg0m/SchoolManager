using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class PaymentMethodDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public PaymentType Type { get; set; }
        public bool IsActive { get; set; }
    }
}
