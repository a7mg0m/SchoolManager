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

        /// <summary>The teacher no longer works at the school (inactive: no teacher app login)</summary>
        public bool IsServiceEnded { get; private set; }

        /// <summary>Last day of service (null while in service)</summary>
        public DateOnly? ServiceEndDate { get; private set; }

        /// <summary>Login name of the teacher's account (the Identity service keeps the login itself)</summary>
        public string? LoginName { get; private set; }

        /// <summary>Job level (المستوى الوظيفي), typed by the school; null when not set</summary>
        public string? JobLevel { get; private set; }

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
            bool isActive,
            AuditInfo auditInfo)
        {
            FullName = fullName;
            DateOfBirth = dateOfBirth;
            NationalId = nationalId;
            Gender = gender;
            PhoneNumber = phoneNumber;
            Email = email;
            Address = address;
            IsActive = isActive;

            AuditInfo = auditInfo;
        }

        public void Update(
            NameLocalized fullName,
            string phoneNumber,
            string? email,
            string address,
            bool isActive,
            AuditInfo auditInfo)
        {
            FullName = fullName;
            IsActive = isActive;
            PhoneNumber = phoneNumber;
            Email = email;
            Address = address;

            AuditInfo = auditInfo;
        }

        /// <summary>
        /// Service ended: the teacher becomes inactive (the Identity service disables their login) and the end date
        /// defaults to today. In service again: the date is cleared (active as chosen in the form).
        /// </summary>
        public void SetService(bool ended, DateOnly? endDate)
        {
            IsServiceEnded = ended;
            if (ended)
            {
                IsActive = false;
                ServiceEndDate = endDate ?? DateOnly.FromDateTime(DateTime.Today);
            }
            else
            {
                ServiceEndDate = null;
            }
        }

        public void SetJobLevel(string? jobLevel)
        {
            JobLevel = string.IsNullOrWhiteSpace(jobLevel) ? null : jobLevel.Trim();
        }

        public void SetLoginName(string? loginName)
        {
            LoginName = string.IsNullOrWhiteSpace(loginName) ? null : loginName.Trim();
        }

        public void Deactivate(AuditInfo auditInfo)        {
            IsActive = false;
            AuditInfo = auditInfo;
        }
    }

}
