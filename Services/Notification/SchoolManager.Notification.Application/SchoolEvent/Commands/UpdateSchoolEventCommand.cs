using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public record UpdateSchoolEventCommand(
        Guid Id,
        string Title,
        string Description,
        DateOnly EventDate,
        TimeOnly StartTime,
        TimeOnly EndTime,
        string? ImageUrl,
        EventForEnum EventFor,
        Guid AcademicYearId,
        Guid SemesterId,
        List<TargetInputDto>? Targets,
        Guid UpdatedBy
    ) : ICommand;
}
