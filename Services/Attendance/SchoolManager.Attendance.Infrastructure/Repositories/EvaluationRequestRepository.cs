using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class EvaluationRequestRepository : BaseRepository<EvaluationRequest>, IEvaluationRequestRepository
    {
        public EvaluationRequestRepository(AttendanceDbContext context) : base(context)
        {
        }
    }
}
