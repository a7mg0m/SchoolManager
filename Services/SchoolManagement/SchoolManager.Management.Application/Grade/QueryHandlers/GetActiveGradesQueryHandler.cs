using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class GetActiveGradesQueryHandler : ICommandHandler<GetActiveGradesQuery, List<GradeDto>>
    {
        private readonly IGradeRepository _repository;
        private readonly IMapper _mapper;

        public GetActiveGradesQueryHandler(IGradeRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<GradeDto>> Handle(GetActiveGradesQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();

            return _mapper.Map<List<GradeDto>>(entities.Where(g => g.IsActive).ToList());
        }
    }
}
