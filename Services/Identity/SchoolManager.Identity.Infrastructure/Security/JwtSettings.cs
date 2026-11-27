namespace SchoolManager.Identity.Infrastructure
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; } = 60;

        /// <summary>
        /// Tokens of the app users (parent / teacher / driver) do not expire in practice (100 years): the user stays
        /// signed in until the school ends it, which the app checks each time it opens (api/user/renew).
        /// </summary>
        public int AppExpiryDays { get; set; } = 36500;
    }
}
