using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateTimetableCommand(
        Guid Id,
        Guid StageId,
        Guid GradeId,
        Guid AcademicYearId,
        List<TimetableSlotItemDto> Slots
    ) : ICommand<bool>;
}
