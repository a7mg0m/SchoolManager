namespace SchoolManager.Financial.Application
{
    public class ExpenseCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
