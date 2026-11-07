

using BuildingBlocks;

namespace SchoolManager.Financial.Domain;

public sealed class Fee : BaseEntity
{
    public Guid GradeId { get; private set; }
    public string Name { get; private set; } = default!;
    public FeeType Type { get; private set; }
    public Money Amount { get; private set; } = default!;
    public bool IsMandatory { get; private set; }
    public AuditInfo AuditInfo { get; private set; } = default!;
    private Fee() { }

    public static Fee Create(Guid gradeId, string name, FeeType type, Money amount, bool isMandatory, AuditInfo auditInfo)
    {
        return new Fee { GradeId = gradeId, Name = name.Trim(), Type = type, Amount = amount, IsMandatory = isMandatory, AuditInfo = auditInfo  };
    }
    public void Update(string name, FeeType type, Money amount, bool isMandatory, AuditInfo auditInfo)
    {
        Name = name.Trim();
        Type = type;
        Amount = amount;
        IsMandatory = isMandatory;
        AuditInfo = auditInfo;
    }
}



