using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>Employee values copied to the User and UserProfile (already trimmed).</summary>
    public sealed record EmployeeAccountData(
        string LoginName,
        string Email,
        string PhoneNumber,
        string Address,
        string FirstName,
        string LastName)
    {
        public static EmployeeAccountData From(string fullNameEn, string email, string phoneNumber, string? address, string loginName)
        {
            var (first, last) = SplitName(fullNameEn);
            return new EmployeeAccountData(
                loginName.Trim(),
                email.Trim(),
                phoneNumber.Trim(),
                address?.Trim() ?? string.Empty,
                first,
                last);
        }

        /// <summary>"Ahmed Ali Hassan" -> ("Ahmed", "Ali Hassan"); a single word is used for both names.</summary>
        private static (string First, string Last) SplitName(string fullName)
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return (string.Empty, string.Empty);

            var first = parts[0];
            var last = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : parts[0];
            return (Truncate(first, 100), Truncate(last, 100));
        }

        private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
    }
}
