using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class UpdateAcademicYearCommandHandler : ICommandHandler<UpdateAcademicYearCommand, bool>
    {
        private readonly IAcademicYearRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ISemesterRepository _semesterRepository;

        private readonly ICurrentUserService _currentUser;

        public UpdateAcademicYearCommandHandler(ICurrentUserService currentUser, IAcademicYearRepository repository, ISemesterRepository semesterRepository, IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _repository = repository;
            _semesterRepository = semesterRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateAcademicYearCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);

            if (entity is null)
                return false;

            var now = DateTime.Now;

            // Keep who/when created; record the update
            entity.Update(
                request.Name,
                request.StartDate,
                request.EndDate,
                request.IsActive,
                entity.AuditInfo.Updated(now, _currentUser.UserId));

            var incoming = request.Semesters ?? new();
            var incomingIds = incoming.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();

            foreach (var existing in entity.Semesters.Where(s => !incomingIds.Contains(s.Id)).ToList())
            {
                entity.Semesters.Remove(existing);
                _semesterRepository.Remove(existing);
            }

            foreach (var dto in incoming)
            {
                var existing = dto.Id.HasValue ? entity.Semesters.FirstOrDefault(s => s.Id == dto.Id.Value) : null;
                // One AuditInfo per semester (owned type – a shared instance would be saved for only one row)
                if (existing is not null)
                    existing.Update(dto.Name, entity.Id, dto.StartDate, dto.EndDate, existing.AuditInfo.Updated(now, _currentUser.UserId));
                else
                    await _semesterRepository.AddAsync(new Semester(dto.Name, entity.Id, dto.StartDate, dto.EndDate, new AuditInfo(now, _currentUser.UserId)));
            }

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
