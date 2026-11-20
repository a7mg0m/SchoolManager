using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllTimetablesQueryHandler : ICommandHandler<GetAllTimetablesQuery, List<TimetableDto>>
	{
		private readonly ITimetableRepository _repository;
		private readonly IMapper _mapper;

		public GetAllTimetablesQueryHandler(ITimetableRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<TimetableDto>> Handle(GetAllTimetablesQuery request, CancellationToken cancellationToken)
		{
			var entities = (await _repository.GetAllAsync())
				.Where(t => request.AcademicYearId is not Guid year || t.AcademicYearId == year)
				// Timetables saved before semesters (no semester) count for every semester
				.Where(t => request.SemesterId is not Guid semester || t.SemesterId == semester || t.SemesterId == null)
				.ToList();

			return _mapper.Map<List<TimetableDto>>(entities);
		}
	}
}
