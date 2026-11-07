using BuildingBlocks;


namespace SchoolManager.Financial.Domain;

public sealed class ExpenseCategory : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public AuditInfo AuditInfo { get; private set; } 
    public ExpenseCategory() { }

    public static ExpenseCategory Create(string name, string? description , AuditInfo auditInfo )
    {
        return new ExpenseCategory {
            Name = name.Trim(),
            Description = description ,
            AuditInfo = auditInfo 
        };
    }
    public void Update(string name, string? description , AuditInfo auditInfo )
    {
        Name = name.Trim();
        Description = description;
        AuditInfo = auditInfo ;
    }
}


