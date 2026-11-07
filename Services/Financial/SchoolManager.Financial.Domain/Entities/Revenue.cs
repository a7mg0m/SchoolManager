using BuildingBlocks;


namespace SchoolManager.Financial.Domain;

public sealed class Revenue : BaseEntity
{
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid CategoryId { get; private set; }
    public RevenueCategory? Category { get; private set; }
    public RevenueType Type { get; private set; }
    public Money Amount { get; private set; } = default!;
    public DateTime RevenueDate { get; private set; }
    public string? Reference { get; private set; }
    public AuditInfo AuditInfo { get; private set; } = default!;

    private Revenue() { }

    public static Revenue Create(string title, string? description, RevenueCategory category, RevenueType type,
        Money amount, DateTime revenueDate, string? reference, AuditInfo auditInfo)
    {

        var revenue = new Revenue
        {
            Title = title.Trim(),
            Description = description,
            CategoryId = category.Id,
            Category = category,
            AuditInfo = auditInfo,
            Type = type,
            Amount = amount,
            RevenueDate = revenueDate,
            Reference = reference
        };
        return revenue;
    }
    public void Update(string title, string? description, RevenueCategory category, RevenueType type,
        Money amount, DateTime revenueDate, string? reference, AuditInfo auditInfo)
    {
        Title = title.Trim();
        Description = description;
        CategoryId = category.Id;
        Category = category;
        Type = type;
        Amount = amount;
        RevenueDate = revenueDate;
        Reference = reference;
        AuditInfo = auditInfo;
    }
}
