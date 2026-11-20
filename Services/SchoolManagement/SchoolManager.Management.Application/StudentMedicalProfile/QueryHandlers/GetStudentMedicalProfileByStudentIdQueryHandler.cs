using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetStudentMedicalProfileByStudentIdQueryHandler : ICommandHandler<GetStudentMedicalProfileByStudentIdQuery, StudentMedicalProfileDto?>
	{
		private readonly IStudentMedicalProfileRepository _repository;
		private readonly IMapper _mapper;

		public GetStudentMedicalProfileByStudentIdQueryHandler(IStudentMedicalProfileRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<StudentMedicalProfileDto?> Handle(GetStudentMedicalProfileByStudentIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByStudentIdAsync(request.StudentId);

			return entity is null ? null : _mapper.Map<StudentMedicalProfileDto>(entity);
		}
	}
}
