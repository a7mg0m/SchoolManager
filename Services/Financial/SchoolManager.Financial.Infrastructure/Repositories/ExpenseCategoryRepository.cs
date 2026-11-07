using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class ExpenseCategoryRepository : BaseRepository<ExpenseCategory>, IExpenseCategoryRepository
    {
        public ExpenseCategoryRepository(FinancialDbContext context) : base(context)
        {
        }
    }
}
