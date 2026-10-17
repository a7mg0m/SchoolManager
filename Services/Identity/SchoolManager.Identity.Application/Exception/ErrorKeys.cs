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
        public const string LoginNameRequired = "LoginNameRequired";
        public const string LoginNameTooShort = "LoginNameTooShort";
        public const string EmailRequired = "EmailRequired";
        public const string EmailInvalid = "EmailInvalid";
        public const string PasswordRequired = "PasswordRequired";
        public const string UserRequired = "UserRequired";
        public const string RoleRequired = "RoleRequired";
        public const string FirstNameRequired = "FirstNameRequired";
        public const string LastNameRequired = "LastNameRequired";
    }
}
