using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace SchoolManager.Management.Domain
{
    public sealed class Student : BaseEntity
    {
        // Basic Info
        public NameLocalized FullName { get; private set; }
        public DateOnly DateOfBirth { get; private set; }
        public string NationalId { get; private set; }   // رقم الهوية / الإقامة
        public string Gender { get; private set; }       // Male / Female

        // Contact Info
        public string PhoneNumber { get; private set; }
        public string? Email { get; private set; }
        public string Address { get; private set; }

        // Academic Info
        public Guid GradeId { get; private set; }
        public Guid SectionId { get; private set; }
        public Guid AcademicYearId { get; private set; }

        // Parent Info
        public Guid ParentId { get; private set; }

        // Status
        public bool IsActive { get; private set; }

        // Navigation Properties
        public Grade? Grade { get; private set; }
        public Section? Section { get; private set; }
        public Parent? Parent { get; private set; }
        public AuditInfo AuditInfo { get; private set; }

        // Constructor
        private Student() { }

        public Student(
            NameLocalized fullName,
            DateOnly dateOfBirth,
            string nationalId,
            string gender,
            string phoneNumber,
            string? email,
            string address,
            Guid gradeId,
            Guid sectionId,
            Guid academicYearId,
            Guid parentId,
            AuditInfo auditInfo)
        {
            FullName = fullName;
            DateOfBirth = dateOfBirth;
            NationalId = nationalId;
            Gender = gender;
            PhoneNumber = phoneNumber;
            Email = email;
            Address = address;
            GradeId = gradeId;
            SectionId = sectionId;
            AcademicYearId = academicYearId;
            ParentId = parentId;
            IsActive = true;
            AuditInfo = auditInfo;
        }

        // Update Method
        public void Update(
            NameLocalized fullName,
            string phoneNumber,
            string? email,
            string address,
            Guid gradeId,
            Guid sectionId,
            Guid academicYearId,
            AuditInfo auditInfo)
        {
            FullName = fullName;
            PhoneNumber = phoneNumber;
            Email = email;
            Address = address;
            GradeId = gradeId;
            SectionId = sectionId;
            AcademicYearId = academicYearId;
            AuditInfo = auditInfo;

        }

        // Deactivate Student
        public void Deactivate(AuditInfo auditInfo)
        {
            IsActive = false;
            AuditInfo = auditInfo;
        }
    }

}
