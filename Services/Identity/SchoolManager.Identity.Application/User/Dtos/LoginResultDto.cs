namespace SchoolManager.Identity.Application
{
    public class LoginResultDto
    {
        public Guid UserId { get; set; }
        public string LoginName { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
