using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class RevenueCategoryRepository : BaseRepository<RevenueCategory>, IRevenueCategoryRepository
    {
        public RevenueCategoryRepository(FinancialDbContext context) : base(context)
        {
        }
    }
}
