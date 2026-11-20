using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Infrastructure
{
    public class BusAssignmentRepository : BaseRepository<BusAssignment>, IBusAssignmentRepository
    {
        public BusAssignmentRepository(AttendanceDbContext context) : base(context)
        {
        }
    }
}
