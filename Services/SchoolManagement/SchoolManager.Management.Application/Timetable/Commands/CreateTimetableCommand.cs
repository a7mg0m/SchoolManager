using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateTimetableCommand(
        Guid StageId,
        Guid GradeId,
        Guid AcademicYearId,
        Guid CreatedBy,
        List<CreateTimetableSlotDto> Slots
    ) : ICommand<Guid>;
}
