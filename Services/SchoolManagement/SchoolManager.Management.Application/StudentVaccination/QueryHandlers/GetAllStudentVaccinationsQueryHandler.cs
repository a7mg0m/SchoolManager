using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllStudentVaccinationsQueryHandler : ICommandHandler<GetAllStudentVaccinationsQuery, List<StudentVaccinationDto>>
	{
		private readonly IStudentVaccinationRepository _repository;
		private readonly IMapper _mapper;

		public GetAllStudentVaccinationsQueryHandler(IStudentVaccinationRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<StudentVaccinationDto>> Handle(GetAllStudentVaccinationsQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<StudentVaccinationDto>>(entities);
		}
	}
}
