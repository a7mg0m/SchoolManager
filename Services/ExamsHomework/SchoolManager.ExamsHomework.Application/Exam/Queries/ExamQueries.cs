using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record GetAllExamsQuery() : IQuery<List<ExamDto>>;

    public record GetExamByIdQuery(Guid Id) : IQuery<ExamDto?>;
}
