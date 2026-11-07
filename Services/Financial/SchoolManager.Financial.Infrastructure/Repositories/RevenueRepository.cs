using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class RevenueRepository : BaseRepository<Revenue>, IRevenueRepository
    {
        public RevenueRepository(FinancialDbContext context) : base(context)
        {
        }
    }
}
