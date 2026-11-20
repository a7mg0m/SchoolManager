using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>Homework published to the child's class this academic year (the child is checked by the controller)</summary>
    public record GetChildHomeworksQuery(
        ParentChild Child,
        IReadOnlyDictionary<Guid, (string Ar, string En)> SubjectNames,
        Guid? SemesterId = null
    ) : IQuery<List<ChildHomeworkDto>>;
}
