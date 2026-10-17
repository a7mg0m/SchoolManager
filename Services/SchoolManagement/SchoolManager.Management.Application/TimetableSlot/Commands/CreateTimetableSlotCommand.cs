using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateTimetableSlotCommand(
        DayOfWeek Day,
        string NameAr,
        string NameEn,
        TimeOnly StartTime,
        TimeOnly EndTime,
        Guid SubjectId,
        Guid TeacherId,
        Guid GradeId,
        Guid SectionId,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
