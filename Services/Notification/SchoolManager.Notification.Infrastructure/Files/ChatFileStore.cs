using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.Infrastructure
{
    /// <summary>
    /// Chat files under {content root}/ChatFiles/{roomId}/{guid}.{ext}: outside wwwroot, so nothing serves them
    /// except the participants-only GET /api/chat/files/{roomId}/{name}.
    /// </summary>
    public partial class ChatFileStore(IWebHostEnvironment environment) : IChatFileStore
    {
        public const string FolderName = "ChatFiles";

        /// <summary>Only names we create: 32 hex characters + an allowed extension (no paths)</summary>
        [GeneratedRegex(@"^[0-9a-f]{32}\.[a-z0-9]{2,5}$")]
        private static partial Regex StoredName();

        private string RoomFolder(Guid roomId) => Path.Combine(environment.ContentRootPath, FolderName, roomId.ToString("N"));

        public async Task<string> SaveAsync(Guid roomId, Stream content, string extension, CancellationToken cancellationToken = default)
        {
            var directory = RoomFolder(roomId);
            Directory.CreateDirectory(directory);

            var name = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            await using (var file = File.Create(Path.Combine(directory, name)))
            {
                await content.CopyToAsync(file, cancellationToken);
            }
            return name;
        }

        public ChatStoredFile? Find(Guid roomId, string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || !StoredName().IsMatch(fileName)) return null;
            if (!ChatMedia.Types.TryGetValue(Path.GetExtension(fileName), out var type)) return null;

            var path = Path.Combine(RoomFolder(roomId), fileName);
            return File.Exists(path) ? new ChatStoredFile(path, type.ContentType) : null;
        }
    }
}
