using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class GetAllHomeworkQuestionsQueryHandler(IHomeworkQuestionRepository repository, IMapper mapper) : IQueryHandler<GetAllHomeworkQuestionsQuery, List<HomeworkQuestionDto>>
    {
        public async Task<List<HomeworkQuestionDto>> Handle(GetAllHomeworkQuestionsQuery request, CancellationToken cancellationToken)
        {
            var items = await repository.GetAllAsync();
            return mapper.Map<List<HomeworkQuestionDto>>(items);
        }
    }

    public class GetHomeworkQuestionByIdQueryHandler(IHomeworkQuestionRepository repository, IMapper mapper) : IQueryHandler<GetHomeworkQuestionByIdQuery, HomeworkQuestionDto?>
    {
        public async Task<HomeworkQuestionDto?> Handle(GetHomeworkQuestionByIdQuery request, CancellationToken cancellationToken)
        {
            var item = await repository.GetByIdAsync(request.Id);
            return item is null ? null : mapper.Map<HomeworkQuestionDto>(item);
        }
    }

    public class GetHomeworkQuestionsByHomeworkIdQueryHandler(IHomeworkQuestionRepository repository, IMapper mapper) : IQueryHandler<GetHomeworkQuestionsByHomeworkIdQuery, List<HomeworkQuestionDto>>
    {
        public async Task<List<HomeworkQuestionDto>> Handle(GetHomeworkQuestionsByHomeworkIdQuery request, CancellationToken cancellationToken)
        {
            var items = await repository.SearchAsync(x => x.HomeworkId == request.HomeworkId);
            return mapper.Map<List<HomeworkQuestionDto>>(items);
        }
    }
}
