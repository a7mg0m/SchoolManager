using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record UpdateHomeworkCommand(
        Guid Id,
        string Title,
        string Description,
        DateOnly DueDate,
        string? AttachmentUrl,
        Guid UpdatedBy
    ) : ICommand;
}
