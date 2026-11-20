using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class StudentEvaluationRepository : BaseRepository<StudentEvaluation>, IStudentEvaluationRepository
    {
        public StudentEvaluationRepository(AttendanceDbContext context) : base(context)
        {
        }
    }
}
