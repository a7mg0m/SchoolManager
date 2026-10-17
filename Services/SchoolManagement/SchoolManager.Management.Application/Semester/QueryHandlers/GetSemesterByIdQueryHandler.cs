using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GetSemesterByIdQueryHandler : ICommandHandler<GetSemesterByIdQuery, SemesterDto?>
	{
		private readonly ISemesterRepository _repository;
		private readonly IMapper _mapper;

		public GetSemesterByIdQueryHandler(ISemesterRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<SemesterDto?> Handle(GetSemesterByIdQuery request, CancellationToken cancellationToken)
		{
			var entity = await _repository.GetByIdAsync(request.Id);

			return entity is null ? null : _mapper.Map<SemesterDto>(entity);
		}
	}
}
