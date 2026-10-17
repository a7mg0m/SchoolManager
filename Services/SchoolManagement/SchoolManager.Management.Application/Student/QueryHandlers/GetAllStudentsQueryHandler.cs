using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllStudentsQueryHandler : ICommandHandler<GetAllStudentsQuery, List<StudentDto>>
	{
		private readonly IStudentRepository _repository;
		private readonly IMapper _mapper;

		public GetAllStudentsQueryHandler(IStudentRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<StudentDto>> Handle(GetAllStudentsQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<StudentDto>>(entities);
		}
	}
}
