using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class GetBusAssignmentByIdQueryHandler : IQueryHandler<GetBusAssignmentByIdQuery, BusAssignmentDto?>
    {
        private readonly IBusAssignmentRepository _repository;
        private readonly IMapper _mapper;

        public GetBusAssignmentByIdQueryHandler(IBusAssignmentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<BusAssignmentDto?> Handle(GetBusAssignmentByIdQuery query, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return entity is null ? null : _mapper.Map<BusAssignmentDto>(entity);
        }
    }

    public class GetAssignmentsByBusQueryHandler : IQueryHandler<GetAssignmentsByBusQuery, List<BusAssignmentDto>>
    {
        private readonly IBusAssignmentRepository _repository;
        private readonly IMapper _mapper;

        public GetAssignmentsByBusQueryHandler(IBusAssignmentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<BusAssignmentDto>> Handle(GetAssignmentsByBusQuery query, CancellationToken cancellationToken)
        {
            var year = query.AcademicYearId;
            var entities = await _repository.SearchAsync(a => a.BusId == query.BusId && (year == null || a.AcademicYearId == year));
            return _mapper.Map<List<BusAssignmentDto>>(entities);
        }
    }

    public class GetAssignmentsByStudentQueryHandler : IQueryHandler<GetAssignmentsByStudentQuery, List<BusAssignmentDto>>
    {
        private readonly IBusAssignmentRepository _repository;
        private readonly IMapper _mapper;

        public GetAssignmentsByStudentQueryHandler(IBusAssignmentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<BusAssignmentDto>> Handle(GetAssignmentsByStudentQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.SearchAsync(a => a.StudentId == query.StudentId);
            return _mapper.Map<List<BusAssignmentDto>>(entities.OrderByDescending(a => a.StartDate).ToList());
        }
    }
}
