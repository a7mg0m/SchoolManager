using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class CreatePaymentMethodDto
    {
        public string Name { get; set; } = string.Empty;
        public PaymentType Type { get; set; }
    }
}
