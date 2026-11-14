namespace SchoolManager.Identity.Application
{
    /// <summary>Small text helpers shared by the employee and employee-group handlers.</summary>
    internal static class EmployeeText
    {
        /// <summary>Trimmed value, or null when empty.</summary>
        public static string? Optional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
