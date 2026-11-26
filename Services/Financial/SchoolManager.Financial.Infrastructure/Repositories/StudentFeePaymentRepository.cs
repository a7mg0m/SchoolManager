using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Infrastructure
{
    public class StudentFeePaymentRepository : BaseRepository<StudentFeePayment>, IStudentFeePaymentRepository
    {
        public StudentFeePaymentRepository(FinancialDbContext context) : base(context)
        {
        }
    }
}
