using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class DeleteSchoolAlbumItemCommandHandler : ICommandHandler<DeleteSchoolAlbumItemCommand, bool>
    {
        private readonly ISchoolAlbumItemRepository _repository;
        private readonly IFileStorage _fileStorage;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteSchoolAlbumItemCommandHandler(ISchoolAlbumItemRepository repository, IFileStorage fileStorage, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _fileStorage = fileStorage;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteSchoolAlbumItemCommand request, CancellationToken cancellationToken)
        {
            var item = await _repository.GetByIdAsync(request.Id);
            if (item is null)
                return false;

            _repository.Remove(item);
            await _unitOfWork.SaveChangesAsync();

            // The row is gone: now the file can go too
            _fileStorage.Delete(item.Url);
            return true;
        }
    }
}
