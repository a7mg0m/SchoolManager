namespace SchoolManager.Identity.Application
{
    public class UpdateMyProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        /// <summary>New image (base64 in JSON). Null keeps the current image.</summary>
        public byte[]? ProfileImage { get; set; }
        /// <summary>True removes the current image.</summary>
        public bool RemoveProfileImage { get; set; }
    }
}
