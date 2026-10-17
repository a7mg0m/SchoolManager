using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class GetAllExamsQueryHandler(IExamRepository repository, IMapper mapper) : IQueryHandler<GetAllExamsQuery, List<ExamDto>>
    {
        public async Task<List<ExamDto>> Handle(GetAllExamsQuery request, CancellationToken cancellationToken)
        {
            var items = await repository.GetAllAsync();
            return mapper.Map<List<ExamDto>>(items);
        }
    }

    public class GetExamByIdQueryHandler(IExamRepository repository, IMapper mapper) : IQueryHandler<GetExamByIdQuery, ExamDto?>
    {
        public async Task<ExamDto?> Handle(GetExamByIdQuery request, CancellationToken cancellationToken)
        {
            var item = await repository.GetByIdAsync(request.Id);
            return item is null ? null : mapper.Map<ExamDto>(item);
        }
    }
}
