using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Identity.Application
{
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message)
        {
        }
    }
    public static class ErrorKeys
    {
        public const string InvalidCredentials = "InvalidCredentials";
        public const string UserInactive = "UserInactive";
        /// <summary>A parent whose children all graduated / were transferred (or who has none)</summary>
        public const string NoCurrentStudents = "NoCurrentStudents";
        /// <summary>The Management service could not say whether the parent has a current child</summary>
        public const string ParentCheckUnavailable = "ParentCheckUnavailable";
        /// <summary>A teacher whose service ended</summary>
        public const string ServiceEnded = "ServiceEnded";
        /// <summary>The Management service could not say whether the teacher is still in service</summary>
        public const string TeacherCheckUnavailable = "TeacherCheckUnavailable";
        /// <summary>The login name the session was opened with was changed (e.g. a bus's): sign in again</summary>
        public const string LoginNameChanged = "LoginNameChanged";
        public const string LoginNameRequired = "LoginNameRequired";
        public const string LoginNameTooShort = "LoginNameTooShort";
        public const string EmailRequired = "EmailRequired";
        public const string EmailInvalid = "EmailInvalid";
        public const string PasswordRequired = "PasswordRequired";
        public const string UserRequired = "UserRequired";
        public const string RoleRequired = "RoleRequired";
        public const string FirstNameRequired = "FirstNameRequired";
        public const string LastNameRequired = "LastNameRequired";

        // Employee groups
        public const string EmployeeGroupRequired = "EmployeeGroupRequired";
        public const string EmployeeGroupNotFound = "EmployeeGroupNotFound";
        public const string EmployeeGroupInactive = "EmployeeGroupInactive";
        public const string EmployeeGroupNameRequired = "EmployeeGroupNameRequired";
        public const string EmployeeGroupNameExists = "EmployeeGroupNameExists";
        public const string EmployeeGroupHasEmployees = "EmployeeGroupHasEmployees";

        // Employees
        public const string FullNameRequired = "FullNameRequired";
        public const string NationalIdRequired = "NationalIdRequired";
        public const string NationalIdExists = "NationalIdExists";
        public const string PhoneNumberRequired = "PhoneNumberRequired";
        public const string JobTitleRequired = "JobTitleRequired";
        public const string HireDateRequired = "HireDateRequired";
        public const string UserNotFound = "UserNotFound";
        public const string LoginNameExists = "LoginNameExists";
        public const string EmailExists = "EmailExists";
        public const string AdminRoleNotFound = "AdminRoleNotFound";

        // My profile / change password
        public const string UserNotAuthenticated = "UserNotAuthenticated";
        public const string CurrentPasswordRequired = "CurrentPasswordRequired";
        public const string CurrentPasswordInvalid = "CurrentPasswordInvalid";
        public const string NewPasswordSameAsCurrent = "NewPasswordSameAsCurrent";
        public const string PasswordTooShort = "PasswordTooShort";
        public const string ProfileImageTooLarge = "ProfileImageTooLarge";

        // Menu items
        public const string MenuItemNotFound = "MenuItemNotFound";
        public const string MenuItemNameRequired = "MenuItemNameRequired";
        public const string MenuItemNameExists = "MenuItemNameExists";
        public const string MenuItemParentNotFound = "MenuItemParentNotFound";
        public const string MenuItemParentInvalid = "MenuItemParentInvalid";
        public const string MenuItemHasChildren = "MenuItemHasChildren";
        public const string MenuItemOrderInvalid = "MenuItemOrderInvalid";
        public const string MenuItemRouteInvalid = "MenuItemRouteInvalid";

        // Group permissions
        public const string GroupPermissionMenuItemRequired = "GroupPermissionMenuItemRequired";
        public const string GroupPermissionDuplicateMenuItem = "GroupPermissionDuplicateMenuItem";
    }
}
