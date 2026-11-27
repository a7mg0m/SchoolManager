using BuildingBlocks;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.Application
{
    public class SaveExamResultsCommandHandler : ICommandHandler<SaveExamResultsCommand, SaveExamResultsSummary?>
    {
        private readonly IExamRepository _examRepository;
        private readonly IExamResultRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IIntegrationEventOutbox _outbox;

        public SaveExamResultsCommandHandler(
            IExamRepository examRepository,
            IExamResultRepository repository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IIntegrationEventOutbox outbox)
        {
            _outbox = outbox;
            _examRepository = examRepository;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<SaveExamResultsSummary?> Handle(SaveExamResultsCommand command, CancellationToken cancellationToken)
        {
            var exam = await _examRepository.GetByIdAsync(command.ExamId);
            if (exam is null)
                return null;

            // A second final: scores only for the students who failed its Final
            if (exam.ExamType == ExamType.SecondFinal)
            {
                var original = exam.OriginalExamId is Guid originalId
                    ? await _examRepository.GetWithResultsAsync(originalId, cancellationToken)
                    : null;
                var failed = original is null ? new Dictionary<Guid, double>() : SecondFinal.FailedStudents(original);
                if (command.Results.Any(r => r.Score is not null && !failed.ContainsKey(r.StudentId)))
                    throw new ExamRuleException(SecondFinal.NotEligible);
            }

            // Existing results of this exam, keyed by student (keep the first if duplicates exist)
            var existing = (await _repository.SearchAsync(x => x.ExamId == command.ExamId))
                .GroupBy(x => x.StudentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            int created = 0, updated = 0, removed = 0;
            var now = DateTime.Now;
            // Students whose score is new or changed: their parents are notified
            var notify = new List<(Guid StudentId, double Score, bool IsCorrection)>();

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
                        new AuditInfo(now, _currentUser.UserId)));
                    created++;
                    notify.Add((item.StudentId, item.Score.Value, false));
                }
                else if (result.Score != item.Score.Value)
                {
                    result.UpdateScore(
                        item.StudentId,
                        item.Score.Value,
                        result.AuditInfo.Updated(now, _currentUser.UserId));
                    await _repository.UpdateAsync(result);
                    updated++;
                    notify.Add((item.StudentId, item.Score.Value, true));
                }
            }

            // One notification per student, for their parents (Notification service, through Kafka; saved with the results)
            foreach (var (studentId, score, isCorrection) in notify)
            {
                _outbox.Enqueue(Topics.ExamsExamResultSaved, exam.Id.ToString(), new ExamResultSavedIntegrationEvent
                {
                    ExamId = exam.Id,
                    ExamType = (int)exam.ExamType,
                    SubjectId = exam.SubjectId,
                    GradeId = exam.GradeId,
                    SectionId = exam.SectionId,
                    AcademicYearId = exam.AcademicYearId,
                    ExamDate = exam.ExamDate,
                    StudentId = studentId,
                    Score = score,
                    Weight = exam.Weight,
                    SuccessScore = exam.SuccessScore,
                    IsCorrection = isCorrection,
                    SavedBy = _currentUser.UserId
                });
            }

            await _unitOfWork.SaveChangesAsync();

            return new SaveExamResultsSummary(created, updated, removed);
        }
    }
}
