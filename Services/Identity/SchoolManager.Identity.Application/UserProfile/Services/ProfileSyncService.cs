using BuildingBlocks;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    /// <summary>
    /// After a UserProfile changes, copies its data to the linked User and Employee so all three stay in sync
    /// (the reverse direction, Employee -> User/UserProfile, is done by <see cref="EmployeeAccountService"/>).
    /// It only stages changes; the calling handler saves everything in one SaveChanges.
    /// </summary>
    public class ProfileSyncService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ICurrentUserService _currentUser;

        public ProfileSyncService(
            IUserRepository userRepository,
            IEmployeeRepository employeeRepository,
            ICurrentUserService currentUser)
        {
            _userRepository = userRepository;
            _employeeRepository = employeeRepository;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Fails with <see cref="ErrorKeys.EmailExists"/> when another user already has the email.
        /// Call before changing anything.
        /// </summary>
        public async Task EnsureEmailAvailableAsync(Guid userId, string email)
        {
            var sameEmail = await _userRepository.SearchAsync(u => u.Email == email);
            if (sameEmail.Any(u => u.Id != userId))
                throw new DomainException(ErrorKeys.EmailExists);
        }

        /// <summary>
        /// Copies the profile's email to the User and its name / contact details to the Employee.
        /// </summary>
        /// <returns>The linked employee, or null when the user is not an employee.</returns>
        public async Task<Employee?> SyncAsync(UserProfile profile, User? user = null)
        {
            var now = DateTime.Now;

            // Login account: email
            user ??= await _userRepository.GetByIdAsync(profile.UserId);
            if (user is not null && !string.Equals(user.Email, profile.Email, StringComparison.OrdinalIgnoreCase))
                user.Update(user.LoginName, profile.Email, user.RoleId, user.AuditInfo.Updated(now, _currentUser.UserId));

            // Employee: English name, email, phone, address
            var employee = (await _employeeRepository.SearchAsync(e => e.UserId == profile.UserId)).FirstOrDefault();
            if (employee is null)
                return null;

            var englishName = string.Join(' ', new[] { profile.FirstName, profile.LastName }
                .Select(n => n?.Trim())
                .Where(n => !string.IsNullOrEmpty(n)));

            employee.Update(
                // The profile has no Arabic name, so the Arabic name stays as entered by the admin.
                // A new NameLocalized instance is used because it is an EF owned type.
                new NameLocalized(employee.FullName.Arabic, string.IsNullOrEmpty(englishName) ? employee.FullName.English : englishName),
                employee.NationalId,
                // Phone is required on employees: keep the current one when the profile has none
                string.IsNullOrWhiteSpace(profile.PhoneNumber) ? employee.PhoneNumber : profile.PhoneNumber.Trim(),
                profile.Email,
                string.IsNullOrWhiteSpace(profile.Address) ? null : profile.Address.Trim(),
                employee.JobTitle,
                employee.HireDate,
                employee.EmployeeGroupId,
                employee.UserId,
                employee.AuditInfo.Updated(now, _currentUser.UserId));

            return employee;
        }
    }
}
