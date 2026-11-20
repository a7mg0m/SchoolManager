using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllStudentMedicalProfilesQueryHandler : ICommandHandler<GetAllStudentMedicalProfilesQuery, List<StudentMedicalProfileDto>>
	{
		private readonly IStudentMedicalProfileRepository _repository;
		private readonly IMapper _mapper;

		public GetAllStudentMedicalProfilesQueryHandler(IStudentMedicalProfileRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<StudentMedicalProfileDto>> Handle(GetAllStudentMedicalProfilesQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<StudentMedicalProfileDto>>(entities);
		}
	}
}
