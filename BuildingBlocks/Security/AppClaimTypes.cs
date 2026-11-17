namespace BuildingBlocks
{
    /// <summary>
    /// Custom JWT claims written by the Identity service at login and read by <see cref="ICurrentUserService"/>.
    /// Each one is only present when the user's profile is linked to that kind of person.
    /// </summary>
    public static class AppClaimTypes
    {
        /// <summary>Id of the Employee (Identity service) the user belongs to.</summary>
        public const string EmployeeId = "employee_id";

        /// <summary>Id of the Teacher (Management service) the user belongs to.</summary>
        public const string TeacherId = "teacher_id";

        /// <summary>Id of the Parent (Management service) the user belongs to.</summary>
        public const string ParentId = "parent_id";
    }
}
