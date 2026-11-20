using BuildingBlocks;

namespace SchoolManager.Management.Domain
{
    public sealed class VaccinationCampaign : BaseEntity
    {
        public Guid VaccineId { get; private set; }
        public string Title { get; private set; }
        public DateOnly StartDate { get; private set; }
        public DateOnly EndDate { get; private set; }
        public bool IsActive { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        // Navigation
        public Vaccine? Vaccine { get; private set; }

        private VaccinationCampaign() { }

        public VaccinationCampaign(Guid vaccineId, string title, DateOnly startDate, DateOnly endDate, AuditInfo auditInfo)
        {
            VaccineId = vaccineId;
            Title = title;
            StartDate = startDate;
            EndDate = endDate;
            IsActive = true;
            AuditInfo = auditInfo;
        }

        public void Update(Guid vaccineId, string title, DateOnly startDate, DateOnly endDate, AuditInfo auditInfo)
        {
            VaccineId = vaccineId;
            Title = title;
            StartDate = startDate;
            EndDate = endDate;
            AuditInfo = auditInfo;
        }

        public void Activate(AuditInfo auditInfo)
        {
            IsActive = true;
            AuditInfo = auditInfo;
        }

        public void Deactivate(AuditInfo auditInfo)
        {
            IsActive = false;
            AuditInfo = auditInfo;
        }
    }
}
