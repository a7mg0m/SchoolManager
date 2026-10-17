using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetTeacherByIdQueryHandler : ICommandHandler<GetTeacherByIdQuery, TeacherDto?>
	{
		private readonly ITeacherRepository _repository;
		private readonly IMapper _mapper;

		public GetTeacherByIdQueryHandler(ITeacherRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<TeacherDto?> Handle(GetTeacherByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			return entity is null ? null : _mapper.Map<TeacherDto>(entity);
		}
	}
}
