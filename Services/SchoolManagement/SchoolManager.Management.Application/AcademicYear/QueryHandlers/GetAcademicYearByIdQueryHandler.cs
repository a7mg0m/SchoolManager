using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAcademicYearByIdQueryHandler : ICommandHandler<GetAcademicYearByIdQuery, AcademicYearDto?>
	{
		private readonly IAcademicYearRepository _repository;
		private readonly IMapper _mapper;

		public GetAcademicYearByIdQueryHandler(IAcademicYearRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<AcademicYearDto?> Handle(GetAcademicYearByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			return entity is null ? null : _mapper.Map<AcademicYearDto>(entity);
		}
	}
}
