using BuildingBlocks;

namespace SchoolManager.Financial.Domain;


public sealed class Expense : BaseEntity
{
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid EventId { get; private set; }
    public Guid CategoryId { get; private set; }
    public ExpenseCategory? Category { get; private set; }
    public ExpenseType Type { get; private set; }
    public Money Amount { get; private set; } = default!;
    public DateTime ExpenseDate { get; private set; }
    public string? Reference { get; private set; }
    /// <summary>Academic year the expense belongs to (null: saved before expenses had a year)</summary>
    public Guid? AcademicYearId { get; private set; }
    public AuditInfo AuditInfo { get; private set; }

    public Expense() { }

    public static Expense Create(string title, string? description,Guid eventId, ExpenseCategory category, ExpenseType type,
        Money amount, DateTime expenseDate, string? reference, AuditInfo auditInfo, Guid? academicYearId = null)
    {
        
        var expense = new Expense
        {
            Title = title.Trim(),
            Description = description,
            EventId =eventId,
            CategoryId = category.Id,
            Category = category,
            AuditInfo = auditInfo ,
            Type = type,
            Amount = amount,
            ExpenseDate = expenseDate,
            Reference = reference,
            AcademicYearId = academicYearId
        };
       
        return expense;
    }
    public void Update(string title, string? description,Guid eventId, ExpenseCategory category, ExpenseType type,
        Money amount, DateTime expenseDate, string? reference, AuditInfo auditInfo, Guid? academicYearId = null)
    {
       
        Title = title.Trim();
        Description = description;
        EventId = eventId;
        CategoryId = category.Id;
        Category = category;
        Type = type;
        Amount = amount;
        ExpenseDate = expenseDate;
        Reference = reference;
        // An expense keeps its year; one saved without a year gets the one sent
        if (AcademicYearId is null && academicYearId is Guid year && year != Guid.Empty)
            AcademicYearId = year;
        AuditInfo = auditInfo ;
       
    }
}
