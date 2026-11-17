using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class DeleteSchoolInfoCommandHandler : ICommandHandler<DeleteSchoolInfoCommand, bool>
    {
        private readonly ISchoolInfoRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorage _fileStorage;

        public DeleteSchoolInfoCommandHandler(ISchoolInfoRepository repository, IUnitOfWork unitOfWork, IFileStorage fileStorage)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
        }

        public async Task<bool> Handle(DeleteSchoolInfoCommand request, CancellationToken cancellationToken)
        {
            // Loaded with its album
            var entity = await _repository.GetByIdAsync(request.Id);

            if (entity is null)
                return false;

            var albumUrls = entity.Album.Select(a => a.Url).ToList();

            // The album rows are deleted with the school info (cascade)
            _repository.Remove(entity);

            await _unitOfWork.SaveChangesAsync();

            // Then the album files
            foreach (var url in albumUrls)
                _fileStorage.Delete(url);

            return true;
        }
    }
}
