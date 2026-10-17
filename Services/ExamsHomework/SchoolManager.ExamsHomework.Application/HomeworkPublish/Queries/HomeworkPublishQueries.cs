using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    public record GetAllHomeworkPublishesQuery() : IQuery<List<HomeworkPublishDto>>;

    public record GetHomeworkPublishByIdQuery(Guid Id) : IQuery<HomeworkPublishDto?>;

    public record GetHomeworkPublishesByHomeworkIdQuery(Guid HomeworkId) : IQuery<List<HomeworkPublishDto>>;
}
