using BuildingBlocks;


namespace SchoolManager.Financial.Domain;

public sealed class RevenueCategory : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public AuditInfo AuditInfo { get; private set; }


    private RevenueCategory() { }

    public static RevenueCategory Create(string name, string? description, AuditInfo auditInfo)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Category name is required.");
        return new RevenueCategory { Name = name.Trim(), Description = description, AuditInfo = auditInfo };
    }
    public void Update(string name, string? description, AuditInfo auditInfo)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Category name is required.");
        Name = name.Trim();
        Description = description;
        AuditInfo = auditInfo;
    }
}
