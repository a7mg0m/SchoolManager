namespace SchoolManager.Management.Application
{
    public class VaccinationCampaignDto
    {
        public Guid Id { get; set; }
        public Guid VaccineId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
