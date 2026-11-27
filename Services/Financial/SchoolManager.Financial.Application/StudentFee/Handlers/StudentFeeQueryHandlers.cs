using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class GetStudentFeeByIdQueryHandler : IQueryHandler<GetStudentFeeByIdQuery, StudentFeeDto?>
    {
        private readonly IStudentFeeRepository _repository;
        private readonly IMapper _mapper;

        public GetStudentFeeByIdQueryHandler(IStudentFeeRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<StudentFeeDto?> Handle(GetStudentFeeByIdQuery query, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return entity is null ? null : _mapper.Map<StudentFeeDto>(entity);
        }
    }

    public class GetStudentFeesByStudentQueryHandler : IQueryHandler<GetStudentFeesByStudentQuery, List<StudentFeeDto>>
    {
        private readonly IStudentFeeRepository _repository;
        private readonly IMapper _mapper;

        public GetStudentFeesByStudentQueryHandler(IStudentFeeRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<StudentFeeDto>> Handle(GetStudentFeesByStudentQuery query, CancellationToken cancellationToken)
        {
            var year = query.AcademicYearId;
            var entities = await _repository.SearchAsync(x => x.StudentId == query.StudentId
                && (year == null || x.AcademicYearId == null || x.AcademicYearId == year));
            return _mapper.Map<List<StudentFeeDto>>(entities);
        }
    }

    public class GetAllStudentFeesQueryHandler : IQueryHandler<GetAllStudentFeesQuery, List<StudentFeeDto>>
    {
        private readonly IStudentFeeRepository _repository;
        private readonly IMapper _mapper;

        public GetAllStudentFeesQueryHandler(IStudentFeeRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<StudentFeeDto>> Handle(GetAllStudentFeesQuery query, CancellationToken cancellationToken)
        {
            var entities = (await _repository.GetAllReadOnlyAsync())
                .Where(x => query.AcademicYearId is not Guid year || x.AcademicYearId == null || x.AcademicYearId == year)
                .ToList();
            return _mapper.Map<List<StudentFeeDto>>(entities);
        }
    }
}
