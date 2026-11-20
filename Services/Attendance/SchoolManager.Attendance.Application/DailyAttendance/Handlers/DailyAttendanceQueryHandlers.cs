using AutoMapper;
using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class GetDailyAttendanceByIdQueryHandler : IQueryHandler<GetDailyAttendanceByIdQuery, DailyAttendanceDto?>
    {
        private readonly IDailyAttendanceRepository _repository;
        private readonly IMapper _mapper;

        public GetDailyAttendanceByIdQueryHandler(IDailyAttendanceRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<DailyAttendanceDto?> Handle(GetDailyAttendanceByIdQuery query, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return entity is null ? null : _mapper.Map<DailyAttendanceDto>(entity);
        }
    }

    public class GetSectionAttendanceByDateQueryHandler : IQueryHandler<GetSectionAttendanceByDateQuery, List<DailyAttendanceDto>>
    {
        private readonly IDailyAttendanceRepository _repository;
        private readonly IMapper _mapper;

        public GetSectionAttendanceByDateQueryHandler(IDailyAttendanceRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<DailyAttendanceDto>> Handle(GetSectionAttendanceByDateQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.SearchAsync(d => d.SectionId == query.SectionId && d.Date == query.Date);
            return _mapper.Map<List<DailyAttendanceDto>>(entities);
        }
    }

    /// <summary>The lesson's rows of every student recorded in it (each with the student of its day)</summary>
    public class GetPeriodAttendanceQueryHandler : IQueryHandler<GetPeriodAttendanceQuery, List<PeriodAttendanceDto>>
    {
        private readonly IDailyAttendanceRepository _repository;
        private readonly IMapper _mapper;

        public GetPeriodAttendanceQueryHandler(IDailyAttendanceRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<PeriodAttendanceDto>> Handle(GetPeriodAttendanceQuery query, CancellationToken cancellationToken)
        {
            var slotId = query.TimetableSlotId;
            var days = await _repository.SearchAsync(d => d.Date == query.Date && d.Periods.Any(p => p.TimetableSlotId == slotId));

            return days
                .SelectMany(day => day.Periods
                    .Where(p => p.TimetableSlotId == slotId)
                    .Select(p =>
                    {
                        var dto = _mapper.Map<PeriodAttendanceDto>(p);
                        dto.StudentId = day.StudentId;
                        return dto;
                    }))
                .ToList();
        }
    }

    public class GetStudentAttendanceQueryHandler : IQueryHandler<GetStudentAttendanceQuery, List<DailyAttendanceDto>>
    {
        private readonly IDailyAttendanceRepository _repository;
        private readonly IMapper _mapper;

        public GetStudentAttendanceQueryHandler(IDailyAttendanceRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<DailyAttendanceDto>> Handle(GetStudentAttendanceQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.SearchAsync(d => d.StudentId == query.StudentId && d.Date >= query.From && d.Date <= query.To);
            return _mapper.Map<List<DailyAttendanceDto>>(entities.OrderBy(d => d.Date).ToList());
        }
    }

    /// <summary>Parent: the child's days (the child must be the parent's); null when Management could not answer</summary>
    public class GetMyChildAttendanceQueryHandler : IQueryHandler<GetMyChildAttendanceQuery, List<DailyAttendanceDto>?>
    {
        private readonly IDailyAttendanceRepository _repository;
        private readonly IParentChildrenService _parentChildren;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public GetMyChildAttendanceQueryHandler(
            ICurrentUserService currentUser,
            IDailyAttendanceRepository repository,
            IParentChildrenService parentChildren,
            IMapper mapper)
        {
            _currentUser = currentUser;
            _repository = repository;
            _parentChildren = parentChildren;
            _mapper = mapper;
        }

        public async Task<List<DailyAttendanceDto>?> Handle(GetMyChildAttendanceQuery query, CancellationToken cancellationToken)
        {
            var children = await ParentChildrenCheck.MyChildrenAsync(_currentUser, _parentChildren, query.Authorization, cancellationToken);
            if (children is null) return null;
            ParentChildrenCheck.EnsureMyChild(children, query.StudentId);

            var entities = await _repository.SearchAsync(d => d.StudentId == query.StudentId && d.Date >= query.From && d.Date <= query.To);
            return _mapper.Map<List<DailyAttendanceDto>>(entities.OrderBy(d => d.Date).ToList());
        }
    }
}
