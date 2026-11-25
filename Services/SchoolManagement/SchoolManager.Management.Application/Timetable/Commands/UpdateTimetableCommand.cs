using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateTimetableCommand(
        Guid Id,
        Guid StageId,
        Guid GradeId,
        Guid AcademicYearId,
        Guid SemesterId,
        List<TimetableSlotItemDto> Slots,
        TimeOnly? DayStartTime = null,
        List<TimetableWorkDayDto>? WorkDays = null
    ) : ICommand<bool>;
}
