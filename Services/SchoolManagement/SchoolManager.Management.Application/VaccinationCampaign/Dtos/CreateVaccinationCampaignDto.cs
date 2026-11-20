namespace SchoolManager.Management.Application
{
    public class CreateVaccinationCampaignDto
    {
        public Guid VaccineId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }
}
