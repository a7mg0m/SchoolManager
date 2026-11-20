namespace SchoolManager.Management.Application
{
    public class SaveStudentMedicalProfileDto
    {
        public Guid StudentId { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? RegularMedications { get; set; }
        public string? EmergencyPhone { get; set; }
        public string? Notes { get; set; }
    }
}
