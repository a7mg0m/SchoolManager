using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class GetAllSchoolInfosQueryHandler : ICommandHandler<GetAllSchoolInfosQuery, List<SchoolInfoDto>>
    {
        private readonly ISchoolInfoRepository _repository;
        private readonly IMapper _mapper;

        public GetAllSchoolInfosQueryHandler(ISchoolInfoRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<SchoolInfoDto>> Handle(GetAllSchoolInfosQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();

            return _mapper.Map<List<SchoolInfoDto>>(entities);
        }
    }
}
