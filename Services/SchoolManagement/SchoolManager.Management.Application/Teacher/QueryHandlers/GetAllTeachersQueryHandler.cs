using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetAllTeachersQueryHandler : ICommandHandler<GetAllTeachersQuery, List<TeacherDto>>
	{
		private readonly ITeacherRepository _repository;
		private readonly IMapper _mapper;

		public GetAllTeachersQueryHandler(ITeacherRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<List<TeacherDto>> Handle(GetAllTeachersQuery request, CancellationToken cancellationToken)
		{
			var entities = await _repository.GetAllAsync();

			return _mapper.Map<List<TeacherDto>>(entities);
		}
	}
}
