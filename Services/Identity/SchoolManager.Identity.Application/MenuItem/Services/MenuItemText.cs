namespace SchoolManager.Identity.Application
{
    internal static class MenuItemText
    {
        /// <summary>Trimmed value, or null when empty.</summary>
        public static string? Optional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
