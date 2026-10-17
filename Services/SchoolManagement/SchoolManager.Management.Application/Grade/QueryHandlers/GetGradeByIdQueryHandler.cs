using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetGradeByIdQueryHandler : ICommandHandler<GetGradeByIdQuery, GradeDto?>
	{
		private readonly IGradeRepository _repository;
		private readonly IMapper _mapper;

		public GetGradeByIdQueryHandler(IGradeRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<GradeDto?> Handle(GetGradeByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			return entity is null ? null : _mapper.Map<GradeDto>(entity);
		}
	}
}
