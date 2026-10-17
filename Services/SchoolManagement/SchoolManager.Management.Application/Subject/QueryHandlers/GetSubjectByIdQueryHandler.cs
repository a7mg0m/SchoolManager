using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetSubjectByIdQueryHandler : ICommandHandler<GetSubjectByIdQuery, SubjectDto?>
	{
		private readonly ISubjectRepository _repository;
		private readonly IMapper _mapper;

		public GetSubjectByIdQueryHandler(ISubjectRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<SubjectDto?> Handle(GetSubjectByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdWithTeachersAsync(request.Id);

			return entity is null ? null : _mapper.Map<SubjectDto>(entity);
		}
	}
}
