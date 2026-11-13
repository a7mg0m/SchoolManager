using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class SaveExamResultsCommandHandler : ICommandHandler<SaveExamResultsCommand, SaveExamResultsSummary?>
    {
        private readonly IExamRepository _examRepository;
        private readonly IExamResultRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public SaveExamResultsCommandHandler(
            IExamRepository examRepository,
            IExamResultRepository repository,
            IUnitOfWork unitOfWork)
        {
            _examRepository = examRepository;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SaveExamResultsSummary?> Handle(SaveExamResultsCommand command, CancellationToken cancellationToken)
        {
            var exam = await _examRepository.GetByIdAsync(command.ExamId);
            if (exam is null)
                return null;

            // Existing results of this exam, keyed by student (keep the first if duplicates exist)
            var existing = (await _repository.SearchAsync(x => x.ExamId == command.ExamId))
                .GroupBy(x => x.StudentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            int created = 0, updated = 0, removed = 0;
            var now = DateTime.Now;

            foreach (var item in command.Results)
            {
                existing.TryGetValue(item.StudentId, out var current);
                var result = current?.FirstOrDefault();

                // Clean up any duplicate rows for this student
                if (current is { Count: > 1 })
                {
                    foreach (var duplicate in current.Skip(1))
                    {
                        _repository.Remove(duplicate);
                        removed++;
                    }
                }

                if (item.Score is null)
                {
                    if (result is not null)
                    {
                        _repository.Remove(result);
                        removed++;
                    }
                    continue;
                }

                if (result is null)
                {
                    // A new AuditInfo per entity: owned instances must not be shared
                    await _repository.AddAsync(new ExamResult(
                        command.ExamId,
                        item.StudentId,
                        item.Score.Value,
                        new AuditInfo(now, command.UserId)));
                    created++;
                }
                else if (result.Score != item.Score.Value)
                {
                    result.UpdateScore(
                        item.StudentId,
                        item.Score.Value,
                        result.AuditInfo.Updated(now, command.UserId));
                    await _repository.UpdateAsync(result);
                    updated++;
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return new SaveExamResultsSummary(created, updated, removed);
        }
    }
}
