using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class GetUserPhotoQueryHandler(IUserProfileRepository profiles) : IQueryHandler<GetUserPhotoQuery, UserPhotoDto?>
    {
        public async Task<UserPhotoDto?> Handle(GetUserPhotoQuery query, CancellationToken cancellationToken)
        {
            var profile = (await profiles.SearchAsync(p => p.UserId == query.UserId)).FirstOrDefault();
            var bytes = profile?.ProfileImage;
            return bytes is { Length: > 0 } ? new UserPhotoDto(bytes, ContentTypeOf(bytes)) : null;
        }

        private static string ContentTypeOf(byte[] b)
        {
            if (b.Length > 3 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
            if (b.Length > 2 && b[0] == 0xFF && b[1] == 0xD8) return "image/jpeg";
            if (b.Length > 3 && b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46) return "image/gif";
            if (b.Length > 11 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return "image/webp";
            return "application/octet-stream";
        }
    }
}
