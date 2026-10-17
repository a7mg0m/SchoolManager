using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllParentsQueryHandler : ICommandHandler<GetAllParentsQuery, List<ParentDto>>
	{
		private readonly IParentRepository _repository;
		private readonly IMapper _mapper;

		public GetAllParentsQueryHandler(IParentRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<ParentDto>> Handle(GetAllParentsQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<ParentDto>>(entities);
		}
	}
}
