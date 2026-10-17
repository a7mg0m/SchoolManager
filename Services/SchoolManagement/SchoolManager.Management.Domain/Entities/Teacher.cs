using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Management.Domain
{
    public sealed class Teacher : BaseEntity
    {
        public NameLocalized FullName { get; private set; }
        public DateOnly DateOfBirth { get; private set; }
        public string NationalId { get; private set; }
        public string Gender { get; private set; }

        public string PhoneNumber { get; private set; }
        public string? Email { get; private set; }
        public string Address { get; private set; }

        public bool IsActive { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        // Navigation
        public List<SubjectTeacher> SubjectTeachers { get; private set; } = new();

        private Teacher() { }

        public Teacher(
            NameLocalized fullName,
            DateOnly dateOfBirth,
            string nationalId,
            string gender,
            string phoneNumber,
            string? email,
            string address,
            AuditInfo auditInfo)
        {
            FullName = fullName;
            DateOfBirth = dateOfBirth;
            NationalId = nationalId;
            Gender = gender;
            PhoneNumber = phoneNumber;
            Email = email;
            Address = address;
            IsActive = true;

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

        public void Deactivate(AuditInfo auditInfo)
        {
            IsActive = false;
            AuditInfo = auditInfo;
        }
    }

}
