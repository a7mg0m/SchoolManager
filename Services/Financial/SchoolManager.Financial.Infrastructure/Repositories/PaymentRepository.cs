using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(FinancialDbContext context) : base(context)
        {
        }
    }
}
