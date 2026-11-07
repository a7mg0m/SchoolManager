using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class PaymentMethodRepository : BaseRepository<PaymentMethod>, IPaymentMethodRepository
    {
        public PaymentMethodRepository(FinancialDbContext context) : base(context)
        {
        }
    }
}
