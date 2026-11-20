using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetStudentVaccinationsByStudentIdQueryHandler : ICommandHandler<GetStudentVaccinationsByStudentIdQuery, List<StudentVaccinationDto>>
	{
		private readonly IStudentVaccinationRepository _repository;
		private readonly IMapper _mapper;

		public GetStudentVaccinationsByStudentIdQueryHandler(IStudentVaccinationRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<StudentVaccinationDto>> Handle(GetStudentVaccinationsByStudentIdQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetByStudentIdAsync(request.StudentId);

			return _mapper.Map<List<StudentVaccinationDto>>(entities);
		}
	}
}
