using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>A parent's login data that cannot be saved (the controller returns <see cref="ErrorKey"/>).</summary>
    public class ParentAccountException : Exception
    {
        public ParentAccountException(string errorKey) : base(errorKey)
        {
            ErrorKey = errorKey;
        }

        public string ErrorKey { get; }
    }

    /// <summary>Checks and prepares a parent's login name and password before the parent is saved.</summary>
    public static class ParentAccount
    {
        public const int LoginNameMinLength = 3;
        public const int LoginNameMaxLength = 100;
        public const int PasswordMinLength = 6;

        public const string LoginNameRequired = "LoginNameRequired";
        public const string LoginNameTooShort = "LoginNameTooShort";
        public const string LoginNameTooLong = "LoginNameTooLong";
        public const string LoginNameExists = "LoginNameExists";
        public const string PasswordRequired = "PasswordRequired";
        public const string PasswordTooShort = "PasswordTooShort";

        /// <summary>
        /// Trimmed login name; throws when it is missing, too short / long, or another parent has it.
        /// (Logins of employees and teachers live in the Identity service: the admin page checks those first.)
        /// </summary>
        public static async Task<string> ValidateLoginNameAsync(
            string? loginName, Guid parentId, IParentRepository repository)
        {
            var value = loginName?.Trim() ?? string.Empty;
            if (value.Length == 0)
                throw new ParentAccountException(LoginNameRequired);
            if (value.Length < LoginNameMinLength)
                throw new ParentAccountException(LoginNameTooShort);
            if (value.Length > LoginNameMaxLength)
                throw new ParentAccountException(LoginNameTooLong);

            var sameName = await repository.SearchAsync(p => p.LoginName == value);
            if (sameName.Any(p => p.Id != parentId))
                throw new ParentAccountException(LoginNameExists);

            return value;
        }

        /// <summary>
        /// BCrypt hash of the password, or null when none was entered and <paramref name="required"/> is false
        /// (keep the current password).
        /// </summary>
        public static string? HashPassword(string? password, bool required)
        {
            if (string.IsNullOrEmpty(password))
                return required ? throw new ParentAccountException(PasswordRequired) : null;
            if (password.Length < PasswordMinLength)
                throw new ParentAccountException(PasswordTooShort);

            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
