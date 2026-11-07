using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class StudentFeeRepository : BaseRepository<StudentFee>, IStudentFeeRepository
    {
        public StudentFeeRepository(FinancialDbContext context) : base(context)
        {
        }
    }
}
