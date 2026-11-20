using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>
    /// Exams of a parent's child (its grade and academic year, for the whole grade or its section) with the child's
    /// own results. The controller has already checked the child belongs to the parent and passes where it studies
    /// and the subject names (both from the Management service).
    /// </summary>
    public record GetChildExamsQuery(
        ParentChild Child,
        IReadOnlyDictionary<Guid, (string Ar, string En)> SubjectNames,
        Guid? SemesterId = null
    ) : IQuery<List<ChildExamDto>>;
}
