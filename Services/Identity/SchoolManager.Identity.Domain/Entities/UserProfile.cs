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

        /// <summary>Employee (this service) the user belongs to, or null.</summary>
        public Guid? EmployeeId { get; private set; }

        /// <summary>Teacher (Management service) the user belongs to, or null. For teachers it equals the user id.</summary>
        public Guid? TeacherId { get; private set; }

        /// <summary>Parent (Management service) the user belongs to, or null.</summary>
        public Guid? ParentId { get; private set; }

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

        /// <summary>Links (or with null unlinks) the employee this user belongs to.</summary>
        public void LinkEmployee(Guid? employeeId) => EmployeeId = Normalize(employeeId);

        /// <summary>Links (or with null unlinks) the teacher this user belongs to.</summary>
        public void LinkTeacher(Guid? teacherId) => TeacherId = Normalize(teacherId);

        /// <summary>Links (or with null unlinks) the parent this user belongs to.</summary>
        public void LinkParent(Guid? parentId) => ParentId = Normalize(parentId);

        private static Guid? Normalize(Guid? id) => id is Guid value && value != Guid.Empty ? value : null;
    }
}
