namespace SchoolManager.Management.Application
{
    /// <summary>A medication given in a clinic visit</summary>
    public class VisitMedicationDto
    {
        public string MedicineName { get; set; } = string.Empty;
        public string Dose { get; set; } = string.Empty;
    }
}
