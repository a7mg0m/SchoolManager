using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateTimetableSlotCommand(
        Guid Id,
        DayOfWeek Day,
        string NameAr,
        string NameEn,
        TimeOnly StartTime,
        TimeOnly EndTime,
        Guid GradeId,
        Guid SectionId,
        Guid SubjectId,
        Guid TeacherId
    ) : ICommand<bool>;
}
