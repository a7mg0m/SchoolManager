using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetParentByIdQueryHandler : ICommandHandler<GetParentByIdQuery, ParentDto?>
	{
		private readonly IParentRepository _repository;
		private readonly IMapper _mapper;

		public GetParentByIdQueryHandler(IParentRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<ParentDto?> Handle(GetParentByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			return entity is null ? null : _mapper.Map<ParentDto>(entity);
		}
	}
}
