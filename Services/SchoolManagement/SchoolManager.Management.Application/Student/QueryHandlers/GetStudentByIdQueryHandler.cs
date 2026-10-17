using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetStudentByIdQueryHandler : ICommandHandler<GetStudentByIdQuery, StudentDto?>
	{
		private readonly IStudentRepository _repository;
		private readonly IMapper _mapper;

		public GetStudentByIdQueryHandler(IStudentRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<StudentDto?> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			return entity is null ? null : _mapper.Map<StudentDto>(entity);
		}
	}
}
