using AutoMapper;
using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class AddSchoolAlbumItemsCommandHandler : ICommandHandler<AddSchoolAlbumItemsCommand, List<SchoolAlbumItemDto>>
    {
        private readonly ISchoolInfoRepository _schoolInfoRepository;
        private readonly ISchoolAlbumItemRepository _albumRepository;
        private readonly IFileStorage _fileStorage;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public AddSchoolAlbumItemsCommandHandler(
            ISchoolInfoRepository schoolInfoRepository,
            ISchoolAlbumItemRepository albumRepository,
            IFileStorage fileStorage,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IMapper mapper)
        {
            _schoolInfoRepository = schoolInfoRepository;
            _albumRepository = albumRepository;
            _fileStorage = fileStorage;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<List<SchoolAlbumItemDto>> Handle(AddSchoolAlbumItemsCommand command, CancellationToken cancellationToken)
        {
            // There is one school info: the album belongs to it (it must be saved first)
            var schoolInfo = (await _schoolInfoRepository.GetAllAsync()).FirstOrDefault()
                ?? throw new SchoolAlbumException(SchoolAlbumRules.SchoolInfoRequired);

            var files = command.Files ?? new List<AlbumFileUpload>();
            if (files.Count == 0)
                throw new SchoolAlbumException(SchoolAlbumRules.NoFiles);
            if (files.Count > SchoolAlbumRules.MaxFilesPerUpload)
                throw new SchoolAlbumException(SchoolAlbumRules.TooManyFiles);

            // Check every file before saving any of them
            var checkedFiles = new List<(AlbumFileUpload File, string Extension, string ContentType)>();
            foreach (var file in files)
                checkedFiles.Add(await CheckAsync(file, cancellationToken));

            var savedUrls = new List<string>();
            try
            {
                var items = new List<SchoolAlbumItem>();
                foreach (var (file, extension, contentType) in checkedFiles)
                {
                    var url = await _fileStorage.SaveAsync(file.Content, SchoolAlbumRules.Folder, extension, cancellationToken);
                    savedUrls.Add(url);

                    var item = new SchoolAlbumItem(
                        schoolInfo.Id,
                        url,
                        CleanFileName(file.FileName),
                        contentType,
                        file.Length,
                        new AuditInfo(DateTime.Now, _currentUser.UserId));
                    await _albumRepository.AddAsync(item);
                    items.Add(item);
                }

                await _unitOfWork.SaveChangesAsync();
                return _mapper.Map<List<SchoolAlbumItemDto>>(items);
            }
            catch
            {
                // Nothing was saved in the database: remove the files written so far
                foreach (var url in savedUrls)
                    _fileStorage.Delete(url);
                throw;
            }
        }

        /// <summary>Extension, size (images 10 MB, videos 100 MB) and real content (the first bytes), then rewind for saving</summary>
        private static async Task<(AlbumFileUpload, string, string)> CheckAsync(AlbumFileUpload file, CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
            if (!SchoolAlbumRules.AllowedTypes.TryGetValue(extension, out var contentType))
                throw new SchoolAlbumException(SchoolAlbumRules.FileTypeNotAllowed, file.FileName);

            if (file.Length <= 0 || file.Length > SchoolAlbumRules.MaxBytesFor(contentType))
                throw new SchoolAlbumException(SchoolAlbumRules.FileTooLarge, file.FileName);

            var head = new byte[12];
            var read = await file.Content.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, cancellationToken);
            if (!SchoolAlbumRules.HasValidSignature(head.AsSpan(0, read), extension))
                throw new SchoolAlbumException(SchoolAlbumRules.FileTypeNotAllowed, file.FileName);

            if (file.Content.CanSeek)
                file.Content.Position = 0;
            else
                throw new InvalidOperationException("Album upload streams must be seekable.");

            return (file, extension, contentType);
        }

        /// <summary>Only the file name (no folders), at most 260 characters</summary>
        private static string CleanFileName(string? fileName)
        {
            var name = Path.GetFileName(fileName ?? string.Empty).Trim();
            if (name.Length == 0)
                name = "file";
            return name.Length <= 260 ? name : name[^260..];
        }
    }
}
