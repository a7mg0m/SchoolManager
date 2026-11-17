using BuildingBlocks.CQRS;

namespace SchoolManager.Notification.Application
{
    /// <summary>
    /// Events that concern a parent's child: for everyone, its stage, grade or section, or the child itself.
    /// The controller has already checked the child belongs to the parent.
    /// </summary>
    public record GetChildEventsQuery(ParentChild Child) : IQuery<List<ChildEventDto>>;
}
