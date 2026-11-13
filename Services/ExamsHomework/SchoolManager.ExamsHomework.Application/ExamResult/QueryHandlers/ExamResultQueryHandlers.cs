using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>
    /// Fills ExamResultDto.IsPassed from each result's exam SuccessScore.
    /// Computed on read so changing an exam's SuccessScore re-grades its results.
    /// </summary>
    internal static class ExamResultPassMapper
    {
        public static async Task<List<ExamResultDto>> MapWithPassAsync(
            IEnumerable<ExamResult> results, IExamRepository examRepository, IMapper mapper)
        {
            var list = results.ToList();
            var examIds = list.Select(r => r.ExamId).Distinct().ToList();
            var exams = (await examRepository.SearchAsync(e => examIds.Contains(e.Id)))
                .ToDictionary(e => e.Id);

            return list.Select(r =>
            {
                var dto = mapper.Map<ExamResultDto>(r);
                dto.IsPassed = exams.TryGetValue(r.ExamId, out var exam) && exam.IsPassed(r.Score);
                return dto;
            }).ToList();
        }
    }

    public class GetAllExamResultsQueryHandler(IExamResultRepository repository, IExamRepository examRepository, IMapper mapper)
        : IQueryHandler<GetAllExamResultsQuery, List<ExamResultDto>>
    {
        public async Task<List<ExamResultDto>> Handle(GetAllExamResultsQuery request, CancellationToken cancellationToken)
        {
            var items = await repository.GetAllAsync();
            return await ExamResultPassMapper.MapWithPassAsync(items, examRepository, mapper);
        }
    }

    public class GetExamResultByIdQueryHandler(IExamResultRepository repository, IExamRepository examRepository, IMapper mapper)
        : IQueryHandler<GetExamResultByIdQuery, ExamResultDto?>
    {
        public async Task<ExamResultDto?> Handle(GetExamResultByIdQuery request, CancellationToken cancellationToken)
        {
            var item = await repository.GetByIdAsync(request.Id);
            if (item is null)
                return null;

            return (await ExamResultPassMapper.MapWithPassAsync(new[] { item }, examRepository, mapper))[0];
        }
    }

    public class GetExamResultsByExamIdQueryHandler(IExamResultRepository repository, IExamRepository examRepository, IMapper mapper)
        : IQueryHandler<GetExamResultsByExamIdQuery, List<ExamResultDto>>
    {
        public async Task<List<ExamResultDto>> Handle(GetExamResultsByExamIdQuery request, CancellationToken cancellationToken)
        {
            var items = await repository.SearchAsync(x => x.ExamId == request.ExamId);
            return await ExamResultPassMapper.MapWithPassAsync(items, examRepository, mapper);
        }
    }
}
