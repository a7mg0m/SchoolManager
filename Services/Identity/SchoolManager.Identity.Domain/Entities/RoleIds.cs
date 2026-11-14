namespace SchoolManager.Identity.Domain
{
    /// <summary>Ids of the seeded roles (see RoleConfiguration / AddRolesWithSeed migration).</summary>
    public static class RoleIds
    {
        public static readonly Guid SystemAdmin = new("00000000-0000-0000-0000-000000000001");
        public static readonly Guid Admin = new("00000000-0000-0000-0000-000000000002");
        public static readonly Guid Teacher = new("00000000-0000-0000-0000-000000000003");
        public static readonly Guid Student = new("00000000-0000-0000-0000-000000000004");
        public static readonly Guid Parent = new("00000000-0000-0000-0000-000000000005");
    }
}
