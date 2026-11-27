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

        // Job information, set by the school (read only in the teacher app)
        public string? JobTitle { get; private set; }
        public string? EmployeeNumber { get; private set; }
        public string? Qualification { get; private set; }
        public string? Specialization { get; private set; }
        public DateOnly? HireDate { get; private set; }
        public int? YearsOfExperience { get; private set; }

        public const int MaxJobTitleLength = 100;
        public const int MaxEmployeeNumberLength = 50;
        public const int MaxQualificationLength = 200;
        public const int MaxSpecializationLength = 200;

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

        /// <summary>The job information (empty texts are cleared)</summary>
        public void SetJobInfo(string? jobTitle, string? employeeNumber, string? qualification, string? specialization,
            DateOnly? hireDate, int? yearsOfExperience)
        {
            static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            JobTitle = Clean(jobTitle);
            EmployeeNumber = Clean(employeeNumber);
            Qualification = Clean(qualification);
            Specialization = Clean(specialization);
            HireDate = hireDate;
            YearsOfExperience = yearsOfExperience;
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
