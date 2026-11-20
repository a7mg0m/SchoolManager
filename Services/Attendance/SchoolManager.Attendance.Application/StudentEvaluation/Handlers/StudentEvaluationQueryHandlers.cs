using AutoMapper;
using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class GetStudentEvaluationByIdQueryHandler : IQueryHandler<GetStudentEvaluationByIdQuery, StudentEvaluationDto?>
    {
        private readonly IStudentEvaluationRepository _repository;
        private readonly IMapper _mapper;

        public GetStudentEvaluationByIdQueryHandler(IStudentEvaluationRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<StudentEvaluationDto?> Handle(GetStudentEvaluationByIdQuery query, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return entity is null ? null : _mapper.Map<StudentEvaluationDto>(entity);
        }
    }

    public class GetEvaluationsBySlotQueryHandler : IQueryHandler<GetEvaluationsBySlotQuery, List<StudentEvaluationDto>>
    {
        private readonly IStudentEvaluationRepository _repository;
        private readonly IMapper _mapper;

        public GetEvaluationsBySlotQueryHandler(IStudentEvaluationRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<StudentEvaluationDto>> Handle(GetEvaluationsBySlotQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.SearchAsync(e => e.TimetableSlotId == query.TimetableSlotId && e.Date == query.Date);
            return _mapper.Map<List<StudentEvaluationDto>>(entities);
        }
    }

    public class GetStudentEvaluationsQueryHandler : IQueryHandler<GetStudentEvaluationsQuery, List<StudentEvaluationDto>>
    {
        private readonly IStudentEvaluationRepository _repository;
        private readonly IMapper _mapper;

        public GetStudentEvaluationsQueryHandler(IStudentEvaluationRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<StudentEvaluationDto>> Handle(GetStudentEvaluationsQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.SearchAsync(e => e.StudentId == query.StudentId && e.Date >= query.From && e.Date <= query.To);
            return _mapper.Map<List<StudentEvaluationDto>>(entities.OrderByDescending(e => e.Date).ToList());
        }
    }

    /// <summary>Parent: the child's evaluations (the child must be the parent's); null when Management could not answer</summary>
    public class GetMyChildEvaluationsQueryHandler : IQueryHandler<GetMyChildEvaluationsQuery, List<StudentEvaluationDto>?>
    {
        private readonly IStudentEvaluationRepository _repository;
        private readonly IParentChildrenService _parentChildren;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public GetMyChildEvaluationsQueryHandler(
            ICurrentUserService currentUser,
            IStudentEvaluationRepository repository,
            IParentChildrenService parentChildren,
            IMapper mapper)
        {
            _currentUser = currentUser;
            _repository = repository;
            _parentChildren = parentChildren;
            _mapper = mapper;
        }

        public async Task<List<StudentEvaluationDto>?> Handle(GetMyChildEvaluationsQuery query, CancellationToken cancellationToken)
        {
            var children = await ParentChildrenCheck.MyChildrenAsync(_currentUser, _parentChildren, query.Authorization, cancellationToken);
            if (children is null) return null;
            ParentChildrenCheck.EnsureMyChild(children, query.StudentId);

            var entities = await _repository.SearchAsync(e => e.StudentId == query.StudentId && e.Date >= query.From && e.Date <= query.To);
            return _mapper.Map<List<StudentEvaluationDto>>(entities.OrderByDescending(e => e.Date).ToList());
        }
    }
}
