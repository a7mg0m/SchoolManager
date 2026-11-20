using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>A teacher's login data that cannot be saved (the controller returns <see cref="ErrorKey"/>).</summary>
    public class TeacherAccountException : Exception
    {
        public TeacherAccountException(string errorKey) : base(errorKey)
        {
            ErrorKey = errorKey;
        }

        public string ErrorKey { get; }
    }

    /// <summary>
    /// Checks and prepares a teacher's login name and password before the teacher is saved
    /// (same rules and error keys as <see cref="ParentAccount"/>).
    /// </summary>
    public static class TeacherAccount
    {
        /// <summary>
        /// Trimmed login name; throws when it is missing, too short / long, or another teacher or a parent has it.
        /// (Logins of employees live in the Identity service: the admin page checks those first.)
        /// </summary>
        public static async Task<string> ValidateLoginNameAsync(
            string? loginName, Guid teacherId, ITeacherRepository teachers, IParentRepository parents)
        {
            var value = loginName?.Trim() ?? string.Empty;
            if (value.Length == 0)
                throw new TeacherAccountException(ParentAccount.LoginNameRequired);
            if (value.Length < ParentAccount.LoginNameMinLength)
                throw new TeacherAccountException(ParentAccount.LoginNameTooShort);
            if (value.Length > ParentAccount.LoginNameMaxLength)
                throw new TeacherAccountException(ParentAccount.LoginNameTooLong);

            var sameName = await teachers.SearchAsync(t => t.LoginName == value);
            if (sameName.Any(t => t.Id != teacherId))
                throw new TeacherAccountException(ParentAccount.LoginNameExists);
            if ((await parents.SearchAsync(p => p.LoginName == value)).Any())
                throw new TeacherAccountException(ParentAccount.LoginNameExists);

            return value;
        }

        /// <summary>
        /// BCrypt hash of the password, or null when none was entered and <paramref name="required"/> is false
        /// (keep the current password). Only the hash leaves this service.
        /// </summary>
        public static string? HashPassword(string? password, bool required)
        {
            if (string.IsNullOrEmpty(password))
                return required ? throw new TeacherAccountException(ParentAccount.PasswordRequired) : null;
            if (password.Length < ParentAccount.PasswordMinLength)
                throw new TeacherAccountException(ParentAccount.PasswordTooShort);

            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
