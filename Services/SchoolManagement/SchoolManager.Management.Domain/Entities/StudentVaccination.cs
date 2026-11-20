using BuildingBlocks;

namespace SchoolManager.Management.Domain
{
    /// <summary>One dose of a vaccine for a student</summary>
    public sealed class StudentVaccination : BaseEntity
    {
        public Guid StudentId { get; private set; }
        public Guid VaccineId { get; private set; }
        public int DoseNumber { get; private set; }
        public VaccinationStatus Status { get; private set; }
        public DateOnly? VaccinationDate { get; private set; }
        public VaccinationPlace Place { get; private set; }
        public Guid? CampaignId { get; private set; }

        /// <summary>Employee (Identity service) who gave the dose; no foreign key</summary>
        public Guid? GivenByEmployeeId { get; private set; }
        public string? Notes { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        // Navigation
        public Student? Student { get; private set; }
        public Vaccine? Vaccine { get; private set; }
        public VaccinationCampaign? Campaign { get; private set; }

        private StudentVaccination() { }

        public StudentVaccination(
            Guid studentId,
            Guid vaccineId,
            int doseNumber,
            VaccinationStatus status,
            DateOnly? vaccinationDate,
            VaccinationPlace place,
            Guid? campaignId,
            Guid? givenByEmployeeId,
            string? notes,
            AuditInfo auditInfo)
        {
            StudentId = studentId;
            VaccineId = vaccineId;
            DoseNumber = doseNumber;
            Status = status;
            VaccinationDate = vaccinationDate;
            Place = place;
            CampaignId = campaignId;
            GivenByEmployeeId = givenByEmployeeId;
            Notes = notes;
            AuditInfo = auditInfo;
        }

        public void Update(
            Guid vaccineId,
            int doseNumber,
            VaccinationStatus status,
            DateOnly? vaccinationDate,
            VaccinationPlace place,
            Guid? campaignId,
            Guid? givenByEmployeeId,
            string? notes,
            AuditInfo auditInfo)
        {
            VaccineId = vaccineId;
            DoseNumber = doseNumber;
            Status = status;
            VaccinationDate = vaccinationDate;
            Place = place;
            CampaignId = campaignId;
            GivenByEmployeeId = givenByEmployeeId;
            Notes = notes;
            AuditInfo = auditInfo;
        }
    }
}
