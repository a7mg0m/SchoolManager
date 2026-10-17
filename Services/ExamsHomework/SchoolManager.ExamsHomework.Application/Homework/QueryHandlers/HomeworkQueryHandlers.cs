using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class GetAllHomeworksQueryHandler(IHomeworkRepository repository, IMapper mapper) : IQueryHandler<GetAllHomeworksQuery, List<HomeworkDto>>
    {
        public async Task<List<HomeworkDto>> Handle(GetAllHomeworksQuery request, CancellationToken cancellationToken)
        {
            var items = await repository.GetAllAsync();
            return mapper.Map<List<HomeworkDto>>(items);
        }
    }

    public class GetHomeworkByIdQueryHandler(IHomeworkRepository repository, IMapper mapper) : IQueryHandler<GetHomeworkByIdQuery, HomeworkDto?>
    {
        public async Task<HomeworkDto?> Handle(GetHomeworkByIdQuery request, CancellationToken cancellationToken)
        {
            var item = await repository.GetByIdAsync(request.Id);
            return item is null ? null : mapper.Map<HomeworkDto>(item);
        }
    }
}
