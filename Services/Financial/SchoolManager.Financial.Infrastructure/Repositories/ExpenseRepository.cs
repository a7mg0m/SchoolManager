using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class ExpenseRepository : BaseRepository<Expense>, IExpenseRepository
    {
        public ExpenseRepository(FinancialDbContext context) : base(context)
        {
        }
    }
}
