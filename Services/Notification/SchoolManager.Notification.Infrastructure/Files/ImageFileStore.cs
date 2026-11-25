using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.Infrastructure
{
    /// <summary>
    /// Saves pictures under wwwroot/notifications and wwwroot/events (wwwroot/default holds the default pictures).
    /// The API serves wwwroot at <see cref="UrlPrefix"/>.
    /// </summary>
    public class ImageFileStore(IWebHostEnvironment environment, ILogger<ImageFileStore> logger) : IImageFileStore
    {
        /// <summary>Public path of the service's wwwroot (gateway route /notification-files/…)</summary>
        public const string UrlPrefix = "/notification-files";

        public static readonly string[] Folders = { "notifications", "events", "default" };

        private static string FolderName(ImageFolder folder) => folder == ImageFolder.Events ? "events" : "notifications";

        /// <summary>wwwroot (created when missing)</summary>
        public static string WebRoot(IWebHostEnvironment environment)
            => environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");

        public async Task<string> SaveAsync(Stream content, string extension, ImageFolder folder, CancellationToken cancellationToken = default)
        {
            var directory = Path.Combine(WebRoot(environment), FolderName(folder));
            Directory.CreateDirectory(directory);

            var name = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            await using (var file = File.Create(Path.Combine(directory, name)))
            {
                await content.CopyToAsync(file, cancellationToken);
            }
            return $"{UrlPrefix}/{FolderName(folder)}/{name}";
        }

        public void Delete(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            // Only our own uploads: never the default pictures or an outside URL
            foreach (var folder in new[] { "notifications", "events" })
            {
                var prefix = $"{UrlPrefix}/{folder}/";
                if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

                var name = Path.GetFileName(url[prefix.Length..]);
                if (string.IsNullOrEmpty(name) || name != url[prefix.Length..]) return; // no sub-folders / "..": not ours

                try
                {
                    var path = Path.Combine(WebRoot(environment), folder, name);
                    if (File.Exists(path)) File.Delete(path);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not delete the picture {Url}", url);
                }
                return;
            }
        }
    }
}
