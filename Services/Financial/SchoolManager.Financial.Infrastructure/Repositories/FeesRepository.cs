using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class FeesRepository : BaseRepository<Fee>, IFeesRepository
    {
        public FeesRepository(FinancialDbContext context) : base(context)
        {
        }
    }
}
