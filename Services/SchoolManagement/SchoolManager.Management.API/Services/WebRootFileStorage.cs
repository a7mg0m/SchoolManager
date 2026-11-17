using SchoolManager.Management.Application;

namespace SchoolManager.Management.API.Services
{
    /// <summary>
    /// Saves uploaded files under wwwroot (served by UseStaticFiles, and through the gateway's /upload route).
    /// File names are new GUIDs, so the uploaded name is never used on disk.
    /// </summary>
    public class WebRootFileStorage : IFileStorage
    {
        private readonly string _webRoot;
        private readonly ILogger<WebRootFileStorage> _logger;

        public WebRootFileStorage(IWebHostEnvironment environment, ILogger<WebRootFileStorage> logger)
        {
            _webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
            _logger = logger;
        }

        public async Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken cancellationToken = default)
        {
            var relativeFolder = folder.Trim('/', '\\').Replace('\\', '/');
            var directory = Path.Combine(_webRoot, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(directory);

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var path = Path.Combine(directory, fileName);

            await using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await content.CopyToAsync(file, cancellationToken);
            }

            return $"/{relativeFolder}/{fileName}";
        }

        public void Delete(string url)
        {
            var path = ToPath(url);
            if (path is null)
            {
                _logger.LogWarning("Not deleting {Url}: it is not a file under wwwroot", url);
                return;
            }

            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not delete {Path}", path);
            }
        }

        /// <summary>Full path of a URL returned by SaveAsync; null when it would point outside wwwroot</summary>
        private string? ToPath(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var relative = url.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            var root = Path.GetFullPath(_webRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(Path.Combine(root, relative));
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full : null;
        }
    }
}
