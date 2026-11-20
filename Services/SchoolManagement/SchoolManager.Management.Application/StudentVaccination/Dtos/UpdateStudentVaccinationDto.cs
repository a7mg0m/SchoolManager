using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class UpdateStudentVaccinationDto
    {
        public Guid Id { get; set; }
        public Guid VaccineId { get; set; }
        public int DoseNumber { get; set; } = 1;
        public VaccinationStatus Status { get; set; }
        public DateOnly? VaccinationDate { get; set; }
        public VaccinationPlace Place { get; set; }
        public Guid? CampaignId { get; set; }
        public Guid? GivenByEmployeeId { get; set; }
        public string? Notes { get; set; }
    }
}
