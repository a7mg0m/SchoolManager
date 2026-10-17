using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllSectionsQueryHandler : ICommandHandler<GetAllSectionsQuery, List<SectionDto>>
	{
		private readonly ISectionRepository _repository;
		private readonly IMapper _mapper;

		public GetAllSectionsQueryHandler(ISectionRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<SectionDto>> Handle(GetAllSectionsQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<SectionDto>>(entities);
		}
	}
}
