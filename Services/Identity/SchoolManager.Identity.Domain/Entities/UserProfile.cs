using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Identity.Domain
{
    public class UserProfile:BaseEntity
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Email { get; private set; }
        public string PhoneNumber { get; private set; }
        public string Address { get; private set; }
        public byte[] ProfileImage { get; private set; }
        public AuditInfo AuditInfo { get; private set; }
        public Guid UserId { get; private set; }
        public User? User { get; private set; }
        private UserProfile()
        {
        }
        public UserProfile(Guid userId, string firstName, string lastName, string email, string phoneNumber, string address, byte[] profileImage, AuditInfo auditInfo)
        {
            UserId = userId;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            Address = address;
            ProfileImage = profileImage;
            AuditInfo = auditInfo;
        }
        public void Update(string firstName, string lastName, string email, string phoneNumber, string address, byte[] profileImage, AuditInfo auditInfo)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            Address = address;
            ProfileImage = profileImage;
            AuditInfo = auditInfo;
        }
    }
}
