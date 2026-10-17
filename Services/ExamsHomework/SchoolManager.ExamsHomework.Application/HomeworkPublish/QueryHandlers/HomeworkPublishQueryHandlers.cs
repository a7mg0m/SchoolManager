using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class GetAllHomeworkPublishesQueryHandler(IHomeworkPublishRepository repository, IMapper mapper) : IQueryHandler<GetAllHomeworkPublishesQuery, List<HomeworkPublishDto>>
    {
        public async Task<List<HomeworkPublishDto>> Handle(GetAllHomeworkPublishesQuery request, CancellationToken cancellationToken)
        {
            var items = await repository.GetAllAsync();
            return mapper.Map<List<HomeworkPublishDto>>(items);
        }
    }

    public class GetHomeworkPublishByIdQueryHandler(IHomeworkPublishRepository repository, IMapper mapper) : IQueryHandler<GetHomeworkPublishByIdQuery, HomeworkPublishDto?>
    {
        public async Task<HomeworkPublishDto?> Handle(GetHomeworkPublishByIdQuery request, CancellationToken cancellationToken)
        {
            var item = await repository.GetByIdAsync(request.Id);
            return item is null ? null : mapper.Map<HomeworkPublishDto>(item);
        }
    }

    public class GetHomeworkPublishesByHomeworkIdQueryHandler(IHomeworkPublishRepository repository, IMapper mapper) : IQueryHandler<GetHomeworkPublishesByHomeworkIdQuery, List<HomeworkPublishDto>>
    {
        public async Task<List<HomeworkPublishDto>> Handle(GetHomeworkPublishesByHomeworkIdQuery request, CancellationToken cancellationToken)
        {
            var items = await repository.SearchAsync(x => x.HomeworkId == request.HomeworkId);
            return mapper.Map<List<HomeworkPublishDto>>(items);
        }
    }
}
