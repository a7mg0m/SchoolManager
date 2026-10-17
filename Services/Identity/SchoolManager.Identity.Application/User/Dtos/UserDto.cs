namespace SchoolManager.Identity.Application
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string LoginName { get; set; } = string.Empty;
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }
        public bool IsReserved { get; set; }
        public Guid RoleId { get; set; }
    }
}
