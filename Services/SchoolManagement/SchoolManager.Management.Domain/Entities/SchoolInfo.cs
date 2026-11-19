using BuildingBlocks;

namespace SchoolManager.Management.Domain
{
    public sealed class SchoolInfo : BaseEntity
    {
        public NameLocalized Name { get; private set; }
        public string? Address { get; private set; }
        public string? Phone { get; private set; }
        public string? Email { get; private set; }
        public byte[]? Logo { get; private set; }

        /// <summary>Location on the map (WGS84 degrees); both set or both null</summary>
        public double? Latitude { get; private set; }
        public double? Longitude { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        /// <summary>School album: URLs of the uploaded files</summary>
        public List<SchoolAlbumItem> Album { get; private set; } = new();

        private SchoolInfo() { }

        public SchoolInfo(NameLocalized name, string? address, string? phone, string? email, byte[]? logo, double? latitude, double? longitude, AuditInfo auditInfo)
        {
            Name = name;
            Address = address;
            Phone = phone;
            Email = email;
            Logo = logo;
            Latitude = latitude;
            Longitude = longitude;
            AuditInfo = auditInfo;
        }

        public void Update(NameLocalized name, string? address, string? phone, string? email, byte[]? logo, double? latitude, double? longitude, AuditInfo auditInfo)
        {
            Name = name;
            Address = address;
            Phone = phone;
            Email = email;
            Logo = logo;
            Latitude = latitude;
            Longitude = longitude;
            AuditInfo = auditInfo;
        }
    }
}
