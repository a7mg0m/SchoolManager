using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateTimetableCommand(
        Guid StageId,
        Guid GradeId,
        Guid AcademicYearId,
        Guid SemesterId,
        List<CreateTimetableSlotDto> Slots,
        TimeOnly? DayStartTime = null,
        List<TimetableWorkDayDto>? WorkDays = null
    ) : ICommand<Guid>;
}
