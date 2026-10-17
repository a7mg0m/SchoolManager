using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetSectionByIdQueryHandler : ICommandHandler<GetSectionByIdQuery, SectionDto?>
	{
		private readonly ISectionRepository _repository;
		private readonly IMapper _mapper;

		public GetSectionByIdQueryHandler(ISectionRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<SectionDto?> Handle(GetSectionByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			return entity is null ? null : _mapper.Map<SectionDto>(entity);
		}
	}
}
