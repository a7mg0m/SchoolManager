using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class GetSchoolInfoByIdQueryHandler : ICommandHandler<GetSchoolInfoByIdQuery, SchoolInfoDto?>
    {
        private readonly ISchoolInfoRepository _repository;
        private readonly IMapper _mapper;

        public GetSchoolInfoByIdQueryHandler(ISchoolInfoRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<SchoolInfoDto?> Handle(GetSchoolInfoByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);

            return entity is null ? null : _mapper.Map<SchoolInfoDto>(entity);
        }
    }
}
