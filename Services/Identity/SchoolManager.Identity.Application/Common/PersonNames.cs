namespace SchoolManager.Identity.Application
{
    /// <summary>Name helpers for profiles built from Management people (teachers, parents).</summary>
    public static class PersonNames
    {
        /// <summary>"Ahmed Ali Hassan" -> ("Ahmed", "Ali Hassan"); falls back to the Arabic name.</summary>
        public static (string First, string Last) Split(string? fullNameEn, string? fullNameAr)
        {
            var source = string.IsNullOrWhiteSpace(fullNameEn) ? fullNameAr : fullNameEn;
            var parts = (source ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return ("-", "-");

            var first = parts[0];
            var last = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : parts[0];
            return (Truncate(first), Truncate(last));
        }

        private static string Truncate(string value) => value.Length <= 100 ? value : value[..100];
    }
}
