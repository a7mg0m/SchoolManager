namespace SchoolManager.Management.Application
{
    public class SchoolInfoDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public byte[]? Logo { get; set; }

        /// <summary>School location on the map (WGS84 degrees); both or neither</summary>
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        /// <summary>School album, oldest first</summary>
        public List<SchoolAlbumItemDto> Album { get; set; } = new();
    }
}
