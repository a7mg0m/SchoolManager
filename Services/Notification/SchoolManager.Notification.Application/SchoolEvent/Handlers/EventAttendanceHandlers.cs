using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class GetEventAttendanceQueryHandler(ISchoolEventRepository events, IEventAttendanceRepository attendance)
        : IQueryHandler<GetEventAttendanceQuery, List<EventAttendanceDto>?>
    {
        public async Task<List<EventAttendanceDto>?> Handle(GetEventAttendanceQuery query, CancellationToken cancellationToken)
        {
            if (await events.GetByIdAsync(query.EventId) is null) return null;

            return (await attendance.SearchAsync(a => a.EventId == query.EventId))
                .Select(a => new EventAttendanceDto(a.AttendeeType, a.AttendeeId, a.IsPresent, a.AuditInfo.UpdatedOn ?? a.AuditInfo.CreatedOn))
                .ToList();
        }
    }

    public class SaveEventAttendanceCommandHandler(
        ISchoolEventRepository events,
        IEventAttendanceRepository attendance,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser) : ICommandHandler<SaveEventAttendanceCommand, bool>
    {
        public async Task<bool> Handle(SaveEventAttendanceCommand command, CancellationToken cancellationToken)
        {
            if (await events.GetByIdAsync(command.EventId) is null) return false;

            var existing = (await attendance.SearchAsync(a => a.EventId == command.EventId))
                .ToDictionary(a => (a.AttendeeType, a.AttendeeId));

            // The last mark of a person wins when the same one is sent twice
            var records = command.Records
                .Where(r => r.AttendeeId != Guid.Empty && Enum.IsDefined(r.AttendeeType))
                .GroupBy(r => (r.AttendeeType, r.AttendeeId))
                .Select(g => g.Last());

            foreach (var record in records)
            {
                existing.TryGetValue((record.AttendeeType, record.AttendeeId), out var mark);

                if (record.IsPresent is not bool present)
                {
                    if (mark is not null) attendance.Remove(mark);
                    continue;
                }

                if (mark is null)
                    await attendance.AddAsync(new EventAttendance(
                        command.EventId, record.AttendeeType, record.AttendeeId, present, new AuditInfo(DateTime.Now, currentUser.UserId)));
                else
                    mark.Mark(present, currentUser.UserId);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
