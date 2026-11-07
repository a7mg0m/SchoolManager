using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class UpdatePaymentMethodDto
    {
        public string Name { get; set; } = string.Empty;
        public PaymentType Type { get; set; }
    }
}
