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

        // Home location (map pin, e.g. for the school bus); both or neither
        public double? Latitude { get; private set; }
        public double? Longitude { get; private set; }

        // Academic Info
        public Guid StageId { get; private set; }
        public Guid GradeId { get; private set; }
        public Guid? SectionId { get; private set; }
        public Guid AcademicYearId { get; private set; }

        // Parent Info
        public Guid ParentId { get; private set; }

        // Status
        public bool IsActive { get; private set; }

        /// <summary>Current / Graduated / Transferred; only a current student is active</summary>
        public StudentStatus Status { get; private set; } = StudentStatus.Current;

        /// <summary>When the student graduated / was transferred (null while current)</summary>
        public DateOnly? StatusDate { get; private set; }

        /// <summary>E.g. the school the student moved to</summary>
        public string? StatusNote { get; private set; }

        public const int MaxStatusNoteLength = 500;

        // Navigation Properties
        public Stage? Stage { get; private set; }
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
            Guid stageId,
            Guid gradeId,
            Guid? sectionId,
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
            StageId = stageId;
            GradeId = gradeId;
            SectionId = sectionId == Guid.Empty ? null : sectionId;
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
            Guid stageId,
            Guid gradeId,
            Guid? sectionId,
            Guid academicYearId,
            AuditInfo auditInfo)
        {
            FullName = fullName;
            PhoneNumber = phoneNumber;
            Email = email;
            Address = address;
            StageId = stageId;
            GradeId = gradeId;
            SectionId = sectionId == Guid.Empty ? null : sectionId;
            AcademicYearId = academicYearId;
            AuditInfo = auditInfo;

        }

        /// <summary>
        /// Promotion after passing the finals: the next grade (and its stage) of the new academic year, with no section
        /// until the student is placed in a class.
        /// </summary>
        public void Promote(Guid stageId, Guid gradeId, Guid academicYearId, AuditInfo auditInfo)
        {
            StageId = stageId;
            GradeId = gradeId;
            SectionId = null;
            AcademicYearId = academicYearId;
            AuditInfo = auditInfo;
        }

        /// <summary>Places the student in a section of their grade (null: no section)</summary>
        public void AssignSection(Guid? sectionId, AuditInfo auditInfo)
        {
            SectionId = sectionId == Guid.Empty ? null : sectionId;
            AuditInfo = auditInfo;
        }

        /// <summary>The student's home location (both values, or neither to clear it)</summary>
        public void SetLocation(double? latitude, double? longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        /// <summary>
        /// Graduated / Transferred: the student leaves the school (no longer active; the date defaults to today).
        /// Current again: active again, the date and note are cleared.
        /// </summary>
        public void ChangeStatus(StudentStatus status, DateOnly? date, string? note, AuditInfo auditInfo)
        {
            if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
            var text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            if (text is not null && text.Length > MaxStatusNoteLength) throw new ArgumentException("StatusNoteTooLong", nameof(note));

            Status = status;
            if (status == StudentStatus.Current)
            {
                IsActive = true;
                StatusDate = null;
                StatusNote = null;
            }
            else
            {
                IsActive = false;
                StatusDate = date ?? DateOnly.FromDateTime(DateTime.Today);
                StatusNote = text;
            }
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
