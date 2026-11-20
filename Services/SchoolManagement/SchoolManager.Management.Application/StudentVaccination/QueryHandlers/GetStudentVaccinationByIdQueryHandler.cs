using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetStudentVaccinationByIdQueryHandler : ICommandHandler<GetStudentVaccinationByIdQuery, StudentVaccinationDto?>
	{
		private readonly IStudentVaccinationRepository _repository;
		private readonly IMapper _mapper;

		public GetStudentVaccinationByIdQueryHandler(IStudentVaccinationRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<StudentVaccinationDto?> Handle(GetStudentVaccinationByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			return entity is null ? null : _mapper.Map<StudentVaccinationDto>(entity);
		}
	}
}
