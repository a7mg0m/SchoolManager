using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Management.Domain
{
    public sealed class Parent : BaseEntity
    {
        public NameLocalized FullName { get; private set; }
        public string PhoneNumber { get; private set; }
        public string? Email { get; private set; }
        public string Address { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        public List<Student> Students { get; private set; } = new();

        private Parent() { }

        public Parent(
            NameLocalized fullName,
            string phoneNumber,
            string? email,
            string address,
            AuditInfo auditInfo)
        {
            FullName = fullName;
            PhoneNumber = phoneNumber;
            Email = email;
            Address = address;

            AuditInfo = auditInfo;
        }

        public void Update(
            NameLocalized fullName,
            string phoneNumber,
            string? email,
            string address,
            AuditInfo auditInfo)
        {
            FullName = fullName;
            PhoneNumber = phoneNumber;
            Email = email;
            Address = address;
            AuditInfo = auditInfo;
        }
    }

}
