using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class UpdateSchoolInfoCommandHandler : ICommandHandler<UpdateSchoolInfoCommand, bool>
    {
        private readonly ISchoolInfoRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSchoolInfoCommandHandler(ISchoolInfoRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateSchoolInfoCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);

            if (entity is null)
                return false;

            entity.Update(
                new NameLocalized(request.NameAr, request.NameEn),
                request.Address,
                request.Phone,
                request.Email,
                request.Logo,                new AuditInfo(DateTime.Now, request.UpdatedBy));

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
