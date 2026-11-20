using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>
    /// Records one lesson for every student sent: the student's day (StudentId + Date) is loaded, or created
    /// when this is the first lesson recorded today (Source = Teacher); RecordPeriod adds the lesson or updates it
    /// when the slot is already recorded. One SaveChanges for the whole class.
    /// </summary>
    public class RecordPeriodAttendanceCommandHandler : ICommandHandler<RecordPeriodAttendanceCommand, bool>
    {
        private readonly IDailyAttendanceRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public RecordPeriodAttendanceCommandHandler(ICurrentUserService currentUser, IDailyAttendanceRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(RecordPeriodAttendanceCommand command, CancellationToken cancellationToken)
        {
            var teacherId = _currentUser.TeacherId
                ?? throw new DomainException("Only a teacher can record lesson attendance.");

            var students = (command.Students ?? new())
                .Where(s => s.StudentId != Guid.Empty)
                .GroupBy(s => s.StudentId)
                .Select(g => g.Last())
                .ToList();
            if (students.Count == 0)
                throw new DomainException("Select at least one student.");

            var studentIds = students.Select(s => s.StudentId).ToList();
            var date = command.Date;
            var days = await _repository.SearchAsync(d => studentIds.Contains(d.StudentId) && d.Date == date);
            var now = DateTime.Now;

            foreach (var student in students)
            {
                var day = days.FirstOrDefault(d => d.StudentId == student.StudentId);
                AuditInfo auditInfo;
                if (day is null)
                {
                    // First lesson of the student's day
                    auditInfo = new AuditInfo(now, _currentUser.UserId);
                    day = DailyAttendance.Create(student.StudentId, command.SectionId, command.GradeId, command.AcademicYearId,
                        date, AttendanceSource.Teacher, auditInfo);
                    await _repository.AddAsync(day);
                }
                else
                {
                    auditInfo = day.AuditInfo.Updated(now, _currentUser.UserId);
                }

                day.RecordPeriod(command.TimetableSlotId, command.SubjectId, teacherId, command.StartTime,
                    student.Status, auditInfo, student.AbsenceReason);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    /// <summary>
    /// The school office sets a student's day from the admin dashboard (Source = Admin): the day is created
    /// when missing, else updated. The check-out time is applied when given.
    /// </summary>
    public class SetDailyAttendanceCommandHandler : ICommandHandler<SetDailyAttendanceCommand, Guid>
    {
        private readonly IDailyAttendanceRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public SetDailyAttendanceCommandHandler(ICurrentUserService currentUser, IDailyAttendanceRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(SetDailyAttendanceCommand command, CancellationToken cancellationToken)
        {
            if (_currentUser.EmployeeId is null)
                throw new DomainException("Only school staff can set a student's attendance.");

            var studentId = command.StudentId;
            var date = command.Date;
            var day = (await _repository.SearchAsync(d => d.StudentId == studentId && d.Date == date)).FirstOrDefault();

            if (day is null)
            {
                var auditInfo = new AuditInfo(DateTime.Now, _currentUser.UserId);
                day = DailyAttendance.Create(command.StudentId, command.SectionId, command.GradeId, command.AcademicYearId,
                    date, AttendanceSource.Admin, auditInfo, command.Status, command.Notes);
                if (command.CheckOutTime is TimeOnly checkOut)
                    day.CheckOut(checkOut, auditInfo);
                await _repository.AddAsync(day);
            }
            else
            {
                var auditInfo = day.AuditInfo.Updated(DateTime.Now, _currentUser.UserId);
                day.SetDayStatus(command.Status, command.Notes, auditInfo);
                if (command.CheckOutTime is TimeOnly checkOut)
                    day.CheckOut(checkOut, auditInfo);
                await _repository.UpdateAsync(day);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return day.Id;
        }
    }

    public class CheckOutStudentCommandHandler : ICommandHandler<CheckOutStudentCommand, bool>
    {
        private readonly IDailyAttendanceRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CheckOutStudentCommandHandler(ICurrentUserService currentUser, IDailyAttendanceRepository repository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(CheckOutStudentCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            entity.CheckOut(command.Time, entity.AuditInfo.Updated(DateTime.Now, _currentUser.UserId));

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public class DeleteDailyAttendanceCommandHandler : ICommandHandler<DeleteDailyAttendanceCommand, bool>
    {
        private readonly IDailyAttendanceRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteDailyAttendanceCommandHandler(IDailyAttendanceRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteDailyAttendanceCommand command, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(command.Id);
            if (entity is null) return false;

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
