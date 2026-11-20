using BuildingBlocks;

namespace SchoolManager.Management.Domain
{
    /// <summary>A visit of a student to the school clinic (examination and diagnosis)</summary>
    public sealed class ClinicVisit : BaseEntity
    {
        public Guid StudentId { get; private set; }

        /// <summary>Name of the doctor who examined the student (typed in)</summary>
        public string DoctorName { get; private set; }
        public DateTime VisitDate { get; private set; }
        public string Complaint { get; private set; }
        public decimal? Temperature { get; private set; }
        public decimal? Weight { get; private set; }
        public decimal? Height { get; private set; }
        public string? Diagnosis { get; private set; }
        public string? Treatment { get; private set; }
        public VisitOutcome Outcome { get; private set; }
        public bool ParentNotified { get; private set; }
        public string? Notes { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        // Navigation
        public Student? Student { get; private set; }
        public List<VisitMedication> Medications { get; private set; } = new();

        private ClinicVisit() { }

        public ClinicVisit(
            Guid studentId,
            string doctorName,
            DateTime visitDate,
            string complaint,
            decimal? temperature,
            decimal? weight,
            decimal? height,
            string? diagnosis,
            string? treatment,
            VisitOutcome outcome,
            bool parentNotified,
            string? notes,
            IEnumerable<(string MedicineName, string Dose)> medications,
            AuditInfo auditInfo)
        {
            StudentId = studentId;
            DoctorName = doctorName;
            VisitDate = visitDate;
            Complaint = complaint;
            Temperature = temperature;
            Weight = weight;
            Height = height;
            Diagnosis = diagnosis;
            Treatment = treatment;
            Outcome = outcome;
            ParentNotified = parentNotified;
            Notes = notes;
            SetMedications(medications);
            AuditInfo = auditInfo;
        }

        public void Update(
            string doctorName,
            DateTime visitDate,
            string complaint,
            decimal? temperature,
            decimal? weight,
            decimal? height,
            string? diagnosis,
            string? treatment,
            VisitOutcome outcome,
            bool parentNotified,
            string? notes,
            IEnumerable<(string MedicineName, string Dose)> medications,
            AuditInfo auditInfo)
        {
            DoctorName = doctorName;
            VisitDate = visitDate;
            Complaint = complaint;
            Temperature = temperature;
            Weight = weight;
            Height = height;
            Diagnosis = diagnosis;
            Treatment = treatment;
            Outcome = outcome;
            ParentNotified = parentNotified;
            Notes = notes;
            SetMedications(medications);
            AuditInfo = auditInfo;
        }

        /// <summary>The medications given in this visit replace the saved list</summary>
        private void SetMedications(IEnumerable<(string MedicineName, string Dose)> medications)
        {
            Medications.Clear();
            foreach (var (medicineName, dose) in medications)
                Medications.Add(new VisitMedication(Id, medicineName, dose));
        }
    }
}
