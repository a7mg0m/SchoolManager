namespace SchoolManager.Identity.Application
{
    public class CreateUserDto
    {
        public string LoginName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string HashPassword { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
    }
}
