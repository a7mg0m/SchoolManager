namespace SchoolManager.Identity.Application
{
    public class EmployeeGroupDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int EmployeesCount { get; set; }
    }
}
