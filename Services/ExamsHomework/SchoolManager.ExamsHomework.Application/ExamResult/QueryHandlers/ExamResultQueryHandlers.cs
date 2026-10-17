using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class GetAllExamResultsQueryHandler(IExamResultRepository repository, IMapper mapper) : IQueryHandler<GetAllExamResultsQuery, List<ExamResultDto>>
    {
        public async Task<List<ExamResultDto>> Handle(GetAllExamResultsQuery request, CancellationToken cancellationToken)
        {
            var items = await repository.GetAllAsync();
            return mapper.Map<List<ExamResultDto>>(items);
        }
    }

    public class GetExamResultByIdQueryHandler(IExamResultRepository repository, IMapper mapper) : IQueryHandler<GetExamResultByIdQuery, ExamResultDto?>
    {
        public async Task<ExamResultDto?> Handle(GetExamResultByIdQuery request, CancellationToken cancellationToken)
        {
            var item = await repository.GetByIdAsync(request.Id);
            return item is null ? null : mapper.Map<ExamResultDto>(item);
        }
    }

    public class GetExamResultsByExamIdQueryHandler(IExamResultRepository repository, IMapper mapper) : IQueryHandler<GetExamResultsByExamIdQuery, List<ExamResultDto>>
    {
        public async Task<List<ExamResultDto>> Handle(GetExamResultsByExamIdQuery request, CancellationToken cancellationToken)
        {
            var items = await repository.SearchAsync(x => x.ExamId == request.ExamId);
            return mapper.Map<List<ExamResultDto>>(items);
        }
    }
}
