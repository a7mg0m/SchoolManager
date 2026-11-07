using BuildingBlocks;
namespace SchoolManager.Financial.Domain;

public sealed class PaymentMethod : BaseEntity
{
    public string Name { get; private set; } = default!;
    public PaymentType Type { get; private set; }
    public bool IsActive { get; private set; } = true;
    public AuditInfo AuditInfo { get; private set; } = default!;

    private PaymentMethod() { }

    public static PaymentMethod Create(string name, PaymentType type, AuditInfo auditInfo) 
    {
        return new PaymentMethod
        {
            Name = name,
            Type = type,
            AuditInfo = auditInfo
        };
    }
    public void Update(string name, PaymentType type)
    {
        Name = name;
        Type = type;
    }
}


