using AutoMapper;
using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class GetMyPendingEvaluationRequestsQueryHandler : IQueryHandler<GetMyPendingEvaluationRequestsQuery, List<EvaluationRequestDto>>
    {
        private readonly IEvaluationRequestRepository _repository;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public GetMyPendingEvaluationRequestsQueryHandler(ICurrentUserService currentUser, IEvaluationRequestRepository repository, IMapper mapper)
        {
            _currentUser = currentUser;
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<EvaluationRequestDto>> Handle(GetMyPendingEvaluationRequestsQuery query, CancellationToken cancellationToken)
        {
            var teacherId = _currentUser.TeacherId
                ?? throw new DomainException("Only a teacher can see evaluation requests.");

            var entities = await _repository.SearchAsync(r => r.TeacherId == teacherId && r.Status == EvaluationRequestStatus.Pending);
            return _mapper.Map<List<EvaluationRequestDto>>(entities.OrderBy(r => r.RequestedAt).ToList());
        }
    }

    /// <summary>The parent's own requests only (ParentId of the logged-in user)</summary>
    public class GetMyEvaluationRequestsQueryHandler : IQueryHandler<GetMyEvaluationRequestsQuery, List<EvaluationRequestDto>>
    {
        private readonly IEvaluationRequestRepository _repository;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public GetMyEvaluationRequestsQueryHandler(ICurrentUserService currentUser, IEvaluationRequestRepository repository, IMapper mapper)
        {
            _currentUser = currentUser;
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<EvaluationRequestDto>> Handle(GetMyEvaluationRequestsQuery query, CancellationToken cancellationToken)
        {
            var parentId = _currentUser.ParentId
                ?? throw new DomainException("Only a parent can see their evaluation requests.");

            var studentId = query.StudentId;
            var entities = await _repository.SearchAsync(r => r.ParentId == parentId && (studentId == null || r.StudentId == studentId));
            return _mapper.Map<List<EvaluationRequestDto>>(entities.OrderByDescending(r => r.RequestedAt).ToList());
        }
    }
}
