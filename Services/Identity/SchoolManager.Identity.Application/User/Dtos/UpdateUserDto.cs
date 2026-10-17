namespace SchoolManager.Identity.Application
{
    public class UpdateUserDto
    {
        public Guid Id { get; set; }
        public string LoginName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
    }
}
