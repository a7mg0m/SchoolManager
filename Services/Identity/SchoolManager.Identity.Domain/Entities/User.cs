using BuildingBlocks;


namespace SchoolManager.Identity.Domain
{
    public class User : BaseEntity
    {
        public string LoginName { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public DateTime? LastLogin { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsReserved { get; private set; }
        public Guid RoleId { get; private set; }
        public Role? Role { get; private set; }
        public AuditInfo AuditInfo { get; private set; }


        public User()
        {

        }
        public User(string loginName, string email, string passwordHash, bool isActive, bool isReserved, DateTime? lastLogin, Guid roleId, AuditInfo auditInfo)
        {
            RoleId = roleId;
            LoginName = loginName;
            Email = email;
            PasswordHash = passwordHash;
            IsActive = isActive;
            IsReserved = isReserved;
            LastLogin = lastLogin;
            AuditInfo = auditInfo;
        }
        public void Update(string loginName, string email, Guid roleId, AuditInfo audit)
        {
            RoleId = roleId;
            LoginName = loginName;
            Email = email;
            AuditInfo = audit;
        }
        public void ChangePassword(string hashPassword, AuditInfo audit)
        {
            PasswordHash = hashPassword;
            AuditInfo = audit;
        }

        public void RecordLogin(DateTime lastLogin)
        {
            LastLogin = lastLogin;
        }

        public void Activate(AuditInfo audit)
        {
            IsActive = true;
            AuditInfo = audit;
        }

        public void Deactivate(AuditInfo audit)
        {
            IsActive = false;
            AuditInfo = audit;
        }
    }
}
