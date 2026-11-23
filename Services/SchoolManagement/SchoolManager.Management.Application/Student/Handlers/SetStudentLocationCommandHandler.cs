using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class SetStudentLocationCommandHandler : ICommandHandler<SetStudentLocationCommand, bool>
    {
        private readonly IStudentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public SetStudentLocationCommandHandler(IStudentRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(SetStudentLocationCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.StudentId);
            if (entity is null)
                return false;

            entity.SetLocation(request.Latitude, request.Longitude);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
