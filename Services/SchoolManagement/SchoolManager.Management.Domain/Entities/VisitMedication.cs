using BuildingBlocks;

namespace SchoolManager.Management.Domain
{
    /// <summary>A medication given in a clinic visit (managed through ClinicVisit)</summary>
    public sealed class VisitMedication : BaseEntity
    {
        public Guid VisitId { get; private set; }
        public string MedicineName { get; private set; }
        public string Dose { get; private set; }

        // Navigation
        public ClinicVisit? Visit { get; private set; }

        private VisitMedication() { }

        public VisitMedication(Guid visitId, string medicineName, string dose)
        {
            VisitId = visitId;
            MedicineName = medicineName;
            Dose = dose;
        }
    }
}
