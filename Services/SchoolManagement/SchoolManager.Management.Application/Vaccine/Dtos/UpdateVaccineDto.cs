namespace SchoolManager.Management.Application
{
    public class UpdateVaccineDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public int TotalDoses { get; set; } = 1;
        public bool IsMandatory { get; set; }
    }
}
