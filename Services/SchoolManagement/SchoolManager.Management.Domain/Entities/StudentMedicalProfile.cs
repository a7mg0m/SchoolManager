using BuildingBlocks;

namespace SchoolManager.Management.Domain
{
    /// <summary>The medical profile of a student (one per student)</summary>
    public sealed class StudentMedicalProfile : BaseEntity
    {
        public Guid StudentId { get; private set; }
        public string? BloodType { get; private set; }
        public string? Allergies { get; private set; }
        public string? ChronicDiseases { get; private set; }
        public string? RegularMedications { get; private set; }
        public string? EmergencyPhone { get; private set; }
        public string? Notes { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        // Navigation
        public Student? Student { get; private set; }

        private StudentMedicalProfile() { }

        public StudentMedicalProfile(
            Guid studentId,
            string? bloodType,
            string? allergies,
            string? chronicDiseases,
            string? regularMedications,
            string? emergencyPhone,
            string? notes,
            AuditInfo auditInfo)
        {
            StudentId = studentId;
            BloodType = bloodType;
            Allergies = allergies;
            ChronicDiseases = chronicDiseases;
            RegularMedications = regularMedications;
            EmergencyPhone = emergencyPhone;
            Notes = notes;
            AuditInfo = auditInfo;
        }

        public void Update(
            string? bloodType,
            string? allergies,
            string? chronicDiseases,
            string? regularMedications,
            string? emergencyPhone,
            string? notes,
            AuditInfo auditInfo)
        {
            BloodType = bloodType;
            Allergies = allergies;
            ChronicDiseases = chronicDiseases;
            RegularMedications = regularMedications;
            EmergencyPhone = emergencyPhone;
            Notes = notes;
            AuditInfo = auditInfo;
        }
    }
}
