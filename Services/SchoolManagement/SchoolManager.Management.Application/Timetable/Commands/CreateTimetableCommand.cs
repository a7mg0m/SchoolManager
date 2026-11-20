using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateTimetableCommand(
        Guid StageId,
        Guid GradeId,
        Guid AcademicYearId,
        Guid SemesterId,
        List<CreateTimetableSlotDto> Slots
    ) : ICommand<Guid>;
}
