using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class CreateSchoolInfoCommandHandler : ICommandHandler<CreateSchoolInfoCommand, Guid>
    {
        private readonly ISchoolInfoRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateSchoolInfoCommandHandler(ISchoolInfoRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateSchoolInfoCommand command, CancellationToken cancellationToken)
        {
            var entity = new Domain.SchoolInfo(
                new NameLocalized(command.NameAr, command.NameEn),
                command.Address,
                command.Phone,
                command.Email,
                command.Logo,                new AuditInfo(DateTime.Now, command.CreatedBy));

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.Id;
        }
    }
}
