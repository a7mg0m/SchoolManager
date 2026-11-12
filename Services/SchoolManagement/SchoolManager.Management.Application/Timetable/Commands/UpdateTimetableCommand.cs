using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateTimetableCommand(
        Guid Id,
        Guid StageId,
        Guid GradeId,
        Guid AcademicYearId,
        Guid UpdatedBy,
        List<TimetableSlotItemDto> Slots
    ) : ICommand<bool>;
}
