

using BuildingBlocks;

namespace SchoolManager.Financial.Domain;

public sealed class Fee : BaseEntity
{
    public Guid GradeId { get; private set; }

    /// <summary>Academic year (Management) the fee is for; null for fees saved before years (every year)</summary>
    public Guid? AcademicYearId { get; private set; }
    public string Name { get; private set; } = default!;
    public FeeType Type { get; private set; }
    public Money Amount { get; private set; } = default!;
    public bool IsMandatory { get; private set; }
    public AuditInfo AuditInfo { get; private set; } = default!;
    private Fee() { }

    public static Fee Create(Guid gradeId, Guid academicYearId, string name, FeeType type, Money amount, bool isMandatory, AuditInfo auditInfo)
    {
        return new Fee { GradeId = gradeId, AcademicYearId = academicYearId, Name = name.Trim(), Type = type, Amount = amount, IsMandatory = isMandatory, AuditInfo = auditInfo  };
    }
    /// <param name="academicYearId">Sets the year of a fee saved before years; a fee that has one keeps it</param>
    public void Update(string name, FeeType type, Money amount, bool isMandatory, AuditInfo auditInfo, Guid? academicYearId = null)
    {
        if (AcademicYearId is null && academicYearId is Guid year && year != Guid.Empty)
            AcademicYearId = year;
        Name = name.Trim();
        Type = type;
        Amount = amount;
        IsMandatory = isMandatory;
        AuditInfo = auditInfo;
    }
}



