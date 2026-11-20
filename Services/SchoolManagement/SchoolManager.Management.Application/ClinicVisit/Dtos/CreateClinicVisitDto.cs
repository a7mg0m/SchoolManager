using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class CreateClinicVisitDto
    {
        public Guid StudentId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateTime VisitDate { get; set; }
        public string Complaint { get; set; } = string.Empty;
        public decimal? Temperature { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public string? Diagnosis { get; set; }
        public string? Treatment { get; set; }
        public VisitOutcome Outcome { get; set; }
        public bool ParentNotified { get; set; }
        public string? Notes { get; set; }
        public List<VisitMedicationDto> Medications { get; set; } = new();
    }
}
