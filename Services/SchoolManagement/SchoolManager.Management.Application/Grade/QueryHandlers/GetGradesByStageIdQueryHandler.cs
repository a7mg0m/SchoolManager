using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class GetGradesByStageIdQueryHandler : ICommandHandler<GetGradesByStageIdQuery, List<GradeDto>>
    {
        private readonly IGradeRepository _repository;
        private readonly IMapper _mapper;

        public GetGradesByStageIdQueryHandler(IGradeRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<GradeDto>> Handle(GetGradesByStageIdQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();

            return _mapper.Map<List<GradeDto>>(entities.Where(x => x.StageId == request.StageId).ToList());
        }
    }
}
