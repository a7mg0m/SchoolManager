using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllAcademicYearsQueryHandler : ICommandHandler<GetAllAcademicYearsQuery, List<AcademicYearDto>>
	{
		private readonly IAcademicYearRepository _repository;
		private readonly IMapper _mapper;

		public GetAllAcademicYearsQueryHandler(IAcademicYearRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<AcademicYearDto>> Handle(GetAllAcademicYearsQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<AcademicYearDto>>(entities);
		}
	}
}
