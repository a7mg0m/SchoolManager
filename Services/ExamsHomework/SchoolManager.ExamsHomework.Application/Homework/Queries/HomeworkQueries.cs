using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record GetAllHomeworksQuery() : IQuery<List<HomeworkDto>>;

    public record GetHomeworkByIdQuery(Guid Id) : IQuery<HomeworkDto?>;
}
